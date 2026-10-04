using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SportsCenterAPI.Data;
using SportsCenterAPI.DTOs.Auth;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models;
using SportsCenterAPI.Services.Implement;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Tests;

public sealed class AuthTokenTests(Flow2Database database)
    : IClassFixture<Flow2Database>, IDisposable
{
    private const string Password = "Auth test password 42!";
    private readonly MemoryCache cache = new(new MemoryCacheOptions());
    private readonly IConfiguration configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "Auth_Tests_Only_Secret_With_At_Least_32_Bytes",
            ["Jwt:Issuer"] = "AuthTokenTests",
            ["Jwt:Audience"] = "AuthTokenTests",
            ["Jwt:ExpireMinutes"] = "15",
            ["Jwt:RefreshTokenExpireDays"] = "7"
        })
        .Build();

    public void Dispose() => cache.Dispose();

    private AuthService Service(AppDbContext db, IEmailService? email = null) =>
        new(db, configuration, cache, email ?? new CapturingEmailService());

    private async Task<User> SeedUserAsync()
    {
        var user = new User
        {
            Email = Guid.NewGuid().ToString("N") + "@example.test",
            FullName = "Auth token test",
            PasswordHash = PasswordHelper.HashPassword(Password),
            Role = "Member",
            IsActive = true
        };
        await using var db = database.Open();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<AuthResponse> LoginAsync(User user, string password = Password)
    {
        await using var db = database.Open();
        return await Service(db).LoginAsync(new()
        {
            Email = user.Email,
            Password = password
        });
    }

    private async Task<AuthResponse> RenewAsync(string key)
    {
        await using var db = database.Open();
        return await Service(db).RenewToken(new() { RefreshTokenKey = key });
    }

    private Task RejectRenewAsync(string key) =>
        Assert.ThrowsAsync<UnauthorizedAccessException>(() => RenewAsync(key));

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    [LocalDbFact]
    public async Task LoginReturnsPairAndStoresOnlyRefreshTokenHash()
    {
        var user = await SeedUserAsync();
        var before = DateTime.UtcNow;
        var response = await LoginAsync(user);

        Assert.Equal(user.Id, response.UserId);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal(64, Convert.FromBase64String(response.RefreshTokenKey).Length);

        await using var db = database.Open();
        var stored = await db.RefreshTokens.SingleAsync(x => x.UserId == user.Id);
        Assert.Equal(Hash(response.RefreshTokenKey), stored.TokenHash);
        Assert.NotEqual(response.RefreshTokenKey, stored.TokenHash);
        Assert.False(stored.IsRevoked);
        Assert.NotEmpty(stored.RowVersion);
        Assert.InRange(stored.CreatedAt, before, DateTime.UtcNow);
        Assert.Equal(stored.ExpiresAt, response.RefreshTokenExpiresAt);
        Assert.InRange(stored.ExpiresAt, before.AddDays(7), DateTime.UtcNow.AddDays(7));
    }

    [LocalDbFact]
    public async Task RenewRotatesTokenAndPreservesSessionExpiration()
    {
        var user = await SeedUserAsync();
        var original = await LoginAsync(user);
        var renewed = await RenewAsync(original.RefreshTokenKey);

        Assert.Equal(user.Id, renewed.UserId);
        Assert.NotEqual(original.Token, renewed.Token);
        Assert.NotEqual(original.RefreshTokenKey, renewed.RefreshTokenKey);
        Assert.Equal(original.RefreshTokenExpiresAt, renewed.RefreshTokenExpiresAt);
        await RejectRenewAsync(original.RefreshTokenKey);

        await using (var db = database.Open())
        {
            var stored = await db.RefreshTokens.SingleAsync(x => x.UserId == user.Id);
            Assert.Equal(Hash(renewed.RefreshTokenKey), stored.TokenHash);
        }

        var next = await RenewAsync(renewed.RefreshTokenKey);
        Assert.Equal(user.Id, next.UserId);
    }

    [LocalDbFact]
    public async Task UnknownExpiredRevokedAndInactiveRefreshTokensAreRejected()
    {
        await RejectRenewAsync(Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));

        foreach (var reason in new[] { "expired", "revoked", "inactive" })
        {
            var user = await SeedUserAsync();
            var response = await LoginAsync(user);

            await using (var db = database.Open())
            {
                var stored = await db.RefreshTokens.SingleAsync(x => x.UserId == user.Id);
                if (reason == "expired") stored.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
                if (reason == "revoked") stored.IsRevoked = true;
                if (reason == "inactive")
                    (await db.Users.SingleAsync(x => x.Id == user.Id)).IsActive = false;
                await db.SaveChangesAsync();
            }

            await RejectRenewAsync(response.RefreshTokenKey);
        }
    }

    [LocalDbFact]
    public async Task LogoutIsIdempotentAndRevokesOnlyTheRequestedSession()
    {
        var user = await SeedUserAsync();
        var firstSession = await LoginAsync(user);
        var otherSession = await LoginAsync(user);

        await using (var db = database.Open())
        {
            var service = Service(db);
            var request = new RefreshTokenDTO { RefreshTokenKey = firstSession.RefreshTokenKey };
            await service.Logout(request);
            await service.Logout(request);
            await service.Logout(new()
            {
                RefreshTokenKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            });
        }

        await RejectRenewAsync(firstSession.RefreshTokenKey);
        Assert.Equal(user.Id, (await RenewAsync(otherSession.RefreshTokenKey)).UserId);
    }

    [LocalDbFact]
    public async Task ConcurrentRefreshRequestsCannotBothConsumeTheSameToken()
    {
        var user = await SeedUserAsync();
        var response = await LoginAsync(user);
        var barrier = new RefreshSaveBarrier(2);

        async Task<AuthResponse?> AttemptAsync()
        {
            await using var db = database.Open(barrier);
            try
            {
                return await Service(db).RenewToken(new()
                {
                    RefreshTokenKey = response.RefreshTokenKey
                });
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        var first = AttemptAsync();
        var second = AttemptAsync();
        try
        {
            await barrier.WaitForArrivalsAsync();
        }
        finally
        {
            barrier.Release();
        }

        var results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(30));
        var winner = Assert.Single(results, x => x is not null)!;
        await RejectRenewAsync(response.RefreshTokenKey);
        Assert.Equal(user.Id, (await RenewAsync(winner.RefreshTokenKey)).UserId);
    }

    [LocalDbFact]
    public async Task LogoutCommittedBeforeRefreshSavePreventsInFlightRefresh()
    {
        var user = await SeedUserAsync();
        var response = await LoginAsync(user);
        var barrier = new RefreshSaveBarrier(1);
        await using var refreshDb = database.Open(barrier);
        var inFlight = Service(refreshDb).RenewToken(new()
        {
            RefreshTokenKey = response.RefreshTokenKey
        });

        try
        {
            await barrier.WaitForArrivalsAsync();
            await using var logoutDb = database.Open();
            await Service(logoutDb).Logout(new()
            {
                RefreshTokenKey = response.RefreshTokenKey
            });
        }
        finally
        {
            barrier.Release();
        }

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => inFlight.WaitAsync(TimeSpan.FromSeconds(30)));
        await RejectRenewAsync(response.RefreshTokenKey);
    }

    [LocalDbFact]
    public async Task PasswordResetRevokesAllUserSessionsButKeepsOtherUsersSignedIn()
    {
        var user = await SeedUserAsync();
        var otherUser = await SeedUserAsync();
        var firstSession = await LoginAsync(user);
        var secondSession = await LoginAsync(user);
        var otherSession = await LoginAsync(otherUser);
        var email = new CapturingEmailService();
        const string newPassword = "Replacement test password 84!";

        await using (var db = database.Open())
        {
            var service = Service(db, email);
            await service.RequestResetPasswordOtpAsync(new() { Email = user.Email });
            Assert.Equal(6, email.Otp.Length);
            await service.VerifyResetPasswordOtpAsync(new()
            {
                Email = user.Email,
                Otp = email.Otp,
                NewPassword = newPassword,
                ConfirmPassword = newPassword
            });
        }

        await RejectRenewAsync(firstSession.RefreshTokenKey);
        await RejectRenewAsync(secondSession.RefreshTokenKey);
        Assert.Equal(otherUser.Id, (await RenewAsync(otherSession.RefreshTokenKey)).UserId);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => LoginAsync(user));
        Assert.Equal(user.Id, (await LoginAsync(user, newPassword)).UserId);

        await using var verify = database.Open();
        var storedUser = await verify.Users.SingleAsync(x => x.Id == user.Id);
        Assert.True(PasswordHelper.VerifyPassword(newPassword, storedUser.PasswordHash));
    }

    private sealed class CapturingEmailService : IEmailService
    {
        public string Otp { get; private set; } = string.Empty;

        public Task SendEmailAsync(string to, string subject, string body)
        {
            Otp = Regex.Match(body, @"\b\d{6}\b").Value;
            return Task.CompletedTask;
        }
    }

    // Pause after each request has read the original rowversion, before any update.
    private sealed class RefreshSaveBarrier(int participants) : SaveChangesInterceptor
    {
        private int arrivals;
        private readonly TaskCompletionSource<bool> ready =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> released =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WaitForArrivalsAsync() => ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
        public void Release() => released.TrySetResult(true);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var rotating = eventData.Context!.ChangeTracker.Entries<RefreshToken>()
                .Any(entry => entry.State == EntityState.Modified &&
                    entry.Property(x => x.TokenHash).IsModified);
            if (rotating)
            {
                if (Interlocked.Increment(ref arrivals) >= participants)
                    ready.TrySetResult(true);
                await released.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }

            return result;
        }
    }
}
