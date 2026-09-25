using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SportsCenterAPI.Data;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models;
using SportsCenterAPI.Models.DTOs.Classes;
using SportsCenterAPI.Services.Implement;

namespace SportsCenterAPI.Tests;

public sealed class Flow2Database : IAsyncLifetime
{
    private readonly string name = "SportsCenter_Flow2Tests_" + Guid.NewGuid().ToString("N");
    public string ConnectionString => $"Server=(localdb)\\MSSQLLocalDB;Database={name};Trusted_Connection=True;TrustServerCertificate=True";
    public AppDbContext Open(SaveChangesInterceptor? interceptor = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(
            ConnectionString);
        if (interceptor != null) options.AddInterceptors(interceptor);
        return new AppDbContext(options.Options);
    }
    public async Task InitializeAsync()
    {
        if (!OperatingSystem.IsWindows()) return;
        await using var db = Open(); await db.Database.MigrateAsync();
    }
    public async Task DisposeAsync()
    {
        if (!OperatingSystem.IsWindows() || !name.StartsWith("SportsCenter_Flow2Tests_", StringComparison.Ordinal)) return;
        await using var db = Open(); await db.Database.EnsureDeletedAsync();
    }
}
public sealed class LocalDbFactAttribute : FactAttribute
{
    public LocalDbFactAttribute() { if (!OperatingSystem.IsWindows()) Skip = "Requires Windows LocalDB."; }
}
public sealed class TestClock : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => UtcNow;
}

public class Flow2Tests(Flow2Database database) : IClassFixture<Flow2Database>
{
    private record Seed(int MemberUser, int Member, int OtherUser, int OtherMember, int Receptionist,
        int CoachUser, int Coach, int OtherCoachUser, int OtherCoach, int Class, int Sport, int Session, int Policy);
    private async Task<Seed> SeedAsync(TestClock clock, int capacity = 2)
    {
        await using var db = database.Open();
        User User(string role) => new() { Email = Guid.NewGuid().ToString("N") + "@example.test", FullName = role + " test", Role = role, PasswordHash = "test-only", IsActive = true };
        var member = new Member { User = User("Member") };
        var other = new Member { User = User("Member") };
        var coach = new Coach { User = User("Coach") };
        var otherCoach = new Coach { User = User("Coach") };
        var reception = User("Receptionist");
        var sport = new Sport { Name = "Flow2 sport" };
        var policy = new CancellationPolicy { Name = "Test two hours", MinimumHoursBeforeStart = 2 };
        var course = new SportClass { ClassName = "Flow2 class", Sport = sport, Coach = coach, StartDate = clock.UtcNow.AddDays(-1).UtcDateTime,
            EndDate = clock.UtcNow.AddDays(30).UtcDateTime, MaxCapacity = capacity, Price = 0 };
        var session = new ClassSession { Class = course, Coach = coach, CancellationPolicy = policy, Capacity = capacity,
            StartsAt = clock.UtcNow.AddDays(1).UtcDateTime, EndsAt = clock.UtcNow.AddDays(1).AddHours(1).UtcDateTime };
        db.AddRange(member, other, reception, otherCoach, session);
        await db.SaveChangesAsync();
        return new(member.UserId, member.Id, other.UserId, other.Id, reception.Id, coach.UserId, coach.Id, otherCoach.UserId, otherCoach.Id, course.Id, sport.Id, session.Id, policy.Id);
    }
    private async Task<RegistrationDTO> Book(Seed s, TestClock clock, bool other = false)
    {
        await using var db = database.Open();
        return await new ClassService(db, clock).BookAsync(other ? s.OtherUser : s.MemberUser, s.Session);
    }
    private static async Task Error(int status, Func<Task> action) => Assert.Equal(status, (await Assert.ThrowsAsync<BusinessException>(action)).StatusCode);

    [LocalDbFact]
    public async Task MemberAndReceptionBookingCancellationCheckInCompletionReview()
    {
        var clock = new TestClock(); var s = await SeedAsync(clock);
        var first = await Book(s, clock);
        Assert.Equal(s.MemberUser, first.CreatedByUserId);
        await using (var db = database.Open())
        {
            var service = new ClassService(db, clock);
            var second = await service.BookAsync(s.Receptionist, s.Session, s.OtherMember);
            Assert.Equal(s.Receptionist, second.CreatedByUserId);
            Assert.Equal(2, (await service.GetRosterAsync(s.CoachUser, s.Session)).Count);
            await service.CancelRegistrationAsync(s.Receptionist, second.Id, new() { Reason = "Khách đổi lịch" });
        }
        var rebooked = await Book(s, clock, true);
        Assert.Equal("Registered", rebooked.Status);
        clock.UtcNow = clock.UtcNow.AddDays(1).AddMinutes(-20);
        await using (var db = database.Open())
        {
            var service = new ClassService(db, clock);
            var attendance = await service.CheckInAsync(s.MemberUser, first.Id);
            Assert.Equal(attendance.Id, (await service.CheckInAsync(s.MemberUser, first.Id)).Id);
            await Error(409, () => service.ReviewAsync(s.MemberUser, first.Id, new() { Rating = 5 }));
        }
        clock.UtcNow = clock.UtcNow.AddHours(2);
        await using (var db = database.Open())
        {
            var service = new ClassService(db, clock);
            await service.CompleteSessionAsync(s.CoachUser, s.Session);
            await service.CompleteSessionAsync(s.CoachUser, s.Session);
            var review = await service.ReviewAsync(s.MemberUser, first.Id, new() { Rating = 5, Comment = "Tốt" });
            Assert.Equal(5, review.Rating);
            await Error(409, () => service.ReviewAsync(s.MemberUser, first.Id, new() { Rating = 4 }));
            await Error(409, () => service.ReviewAsync(s.OtherUser, rebooked.Id, new() { Rating = 5 }));
            Assert.Single((await service.GetReviewsAsync(s.Session, 1, 20)).Items);
            Assert.Equal(2, (await service.GetRosterAsync(s.CoachUser, s.Session)).Count);
            Assert.True(await db.Notifications.AnyAsync(n => n.UserId == s.MemberUser));
            Assert.True(await db.Notifications.AnyAsync(n => n.UserId == s.CoachUser));
            Assert.True(await db.Notifications.AnyAsync(n => n.UserId == 1));
        }
    }

    [LocalDbFact]
    public async Task DuplicateAndFullSessionAreRejectedAndCancellationFreesSeat()
    {
        var clock = new TestClock(); var s = await SeedAsync(clock, 1); var booking = await Book(s, clock);
        await Error(409, () => Book(s, clock)); await Error(409, () => Book(s, clock, true));
        await using (var db = database.Open())
        {
            var service = new ClassService(db, clock);
            var session = Assert.Single((await service.SearchSessionsAsync(new(), s.Class)).Items);
            Assert.Equal(1, session.CurrentEnrollment); Assert.Equal(0, session.AvailableSeats);
            await service.CancelRegistrationAsync(s.MemberUser, booking.Id, new() { Reason = "Đổi lịch" });
            await service.CancelRegistrationAsync(s.MemberUser, booking.Id, new() { Reason = "Lặp yêu cầu" });
        }
        await Book(s, clock, true);
        await using var verify = database.Open();
        Assert.Equal(1, await verify.ClassRegistrations.CountAsync(r => r.SessionId == s.Session && r.Status == "Registered"));
    }

    [LocalDbFact]
    public async Task ConcurrentRequestsCannotOversellLastSeat()
    {
        var clock = new TestClock(); var s = await SeedAsync(clock, 1);
        async Task<bool> Attempt(bool other)
        {
            try { await Book(s, clock, other); return true; }
            catch (BusinessException e) when (e.StatusCode == 409) { return false; }
        }
        var results = await Task.WhenAll(Attempt(false), Attempt(true));
        Assert.Single(results, x => x);
        await using var db = database.Open();
        Assert.Equal(1, await db.ClassRegistrations.CountAsync(r => r.SessionId == s.Session && r.Status == "Registered"));
    }

    [LocalDbFact]
    public async Task CancellationBoundaryAndPolicySnapshotAreEnforced()
    {
        var clock = new TestClock(); var s = await SeedAsync(clock); var booking = await Book(s, clock);
        await using (var db = database.Open())
            await new ClassService(db, clock).SavePolicyAsync(1, s.Policy, new() { Name = "Now 4 hours", MinimumHoursBeforeStart = 4 });
        clock.UtcNow = new DateTimeOffset(booking.CancellationDeadline!.Value, TimeSpan.Zero);
        await using (var db = database.Open())
            await new ClassService(db, clock).CancelRegistrationAsync(s.MemberUser, booking.Id, new() { Reason = "Đúng hạn" });
        var late = await Book(s, clock, true); // Current policy's 4-hour deadline is already past.
        await using (var db = database.Open())
            await Error(409, () => new ClassService(db, clock).CancelRegistrationAsync(s.OtherUser, late.Id, new() { Reason = "Muộn" }));
    }

    [LocalDbFact]
    public async Task OwnershipAndAssignedCoachProtectPrivateOperations()
    {
        var clock = new TestClock(); var s = await SeedAsync(clock); var booking = await Book(s, clock);
        await using var db = database.Open(); var service = new ClassService(db, clock);
        await Error(403, () => service.BookAsync(s.MemberUser, s.Session, s.OtherMember));
        await Error(403, () => service.GetRegistrationsAsync(s.MemberUser, s.OtherMember, 1, 20));
        await Error(404, () => service.CancelRegistrationAsync(s.OtherUser, booking.Id, new() { Reason = "Không phải chủ" }));
        await Error(404, () => service.CheckInAsync(s.OtherUser, booking.Id));
        await Error(403, () => service.GetRosterAsync(s.OtherCoachUser, s.Session));
        await Error(403, () => service.CheckInAsync(s.OtherCoachUser, booking.Id));
        await Error(403, () => service.CompleteSessionAsync(s.OtherCoachUser, s.Session));
        await Error(403, () => service.CancelSessionAsync(s.CoachUser, s.Session, new() { Reason = "Không phải Manager" }));
    }

    [LocalDbFact]
    public async Task ManagerCanManageCalendarButCannotShrinkOrRescheduleBookedSession()
    {
        var clock = new TestClock(); var s = await SeedAsync(clock); await Book(s, clock); await Book(s, clock, true);
        SessionRequest Request() => new() { CoachId = s.Coach, CancellationPolicyId = s.Policy, StartsAt = clock.UtcNow.AddDays(1), EndsAt = clock.UtcNow.AddDays(1).AddHours(1), Capacity = 3 };
        await using (var db = database.Open())
        {
            var service = new ClassService(db, clock);
            Assert.Equal(3, (await service.SaveSessionAsync(1, s.Class, s.Session, Request())).Capacity);
        }
        await using (var db = database.Open())
        {
            var service = new ClassService(db, clock); var request = Request(); request.Capacity = 1;
            await Error(409, () => service.SaveSessionAsync(1, s.Class, s.Session, request));
            request = Request(); request.StartsAt = request.StartsAt.AddMinutes(1);
            await Error(409, () => service.SaveSessionAsync(1, s.Class, s.Session, request));
            await Error(409, () => service.SaveSessionAsync(1, s.Class, null, Request())); // HLV overlap.
            await Error(409, () => service.DeactivateClassAsync(1, s.Class));
            await Error(400, () => service.SavePolicyAsync(1, null, new() { Name = "Invalid", MinimumHoursBeforeStart = 1 }));
            await service.CancelSessionAsync(1, s.Session, new() { Reason = "Trung tâm đóng cửa" });
            Assert.Empty(await service.GetRosterAsync(s.CoachUser, s.Session));
            await service.DeactivateClassAsync(1, s.Class);
        }
        await Error(409, () => Book(s, clock));
    }

    [LocalDbFact]
    public async Task CheckInWindowAndCompletionTimeAreEnforced()
    {
        var clock = new TestClock(); var s = await SeedAsync(clock); var booking = await Book(s, clock);
        await using (var db = database.Open())
        {
            var service = new ClassService(db, clock);
            await Error(409, () => service.CheckInAsync(s.MemberUser, booking.Id));
            await Error(409, () => service.CompleteSessionAsync(s.CoachUser, s.Session));
        }
        clock.UtcNow = clock.UtcNow.AddDays(1).AddMinutes(-30);
        await using (var db = database.Open())
            Assert.Equal("Present", (await new ClassService(db, clock).CheckInAsync(s.CoachUser, booking.Id)).Status);
    }

    [LocalDbFact]
    public async Task BookingDoesNotRequireMembershipOrMemberTimeConflictRule()
    {
        var clock = new TestClock(); var s = await SeedAsync(clock); await Book(s, clock);
        await using var db = database.Open(); var service = new ClassService(db, clock);
        Assert.False(await db.MemberSubscriptions.AnyAsync(x => x.MemberId == s.Member));
        var second = await service.SaveSessionAsync(1, s.Class, null, new()
        {
            CoachId = s.OtherCoach, CancellationPolicyId = s.Policy, Capacity = 3,
            StartsAt = clock.UtcNow.AddDays(1), EndsAt = clock.UtcNow.AddDays(1).AddHours(1)
        });
        Assert.Equal("Registered", (await service.BookAsync(s.MemberUser, second.Id)).Status);
    }

    private sealed class FailNotifications : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<Notification>().Any(e => e.State == EntityState.Added))
                throw new InvalidOperationException("Simulated notification persistence failure");
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
    [LocalDbFact]
    public async Task NotificationFailureRollsBackRegistrationAndSeat()
    {
        var clock = new TestClock(); var s = await SeedAsync(clock, 1);
        await using (var db = database.Open(new FailNotifications()))
            await Assert.ThrowsAsync<InvalidOperationException>(() => new ClassService(db, clock).BookAsync(s.MemberUser, s.Session));
        await using (var db = database.Open()) Assert.False(await db.ClassRegistrations.AnyAsync(r => r.SessionId == s.Session));
        await Book(s, clock, true);
    }
}
