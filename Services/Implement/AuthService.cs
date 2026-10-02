using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SportsCenterAPI.Data;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models;
using SportsCenterAPI.Models.DTOs.Accounts;
using SportsCenterAPI.Models.DTOs.Auth;
using SportsCenterAPI.Models.DTOs.Login;
using SportsCenterAPI.Models.DTOs.Register;
using SportsCenterAPI.Services.Interface;
using System.Security.Cryptography;
using System.Text;

namespace SportsCenterAPI.Services.Implement;

public class AuthService(
    AppDbContext context,
    IConfiguration configuration,
    IMemoryCache cache,
    IEmailService emailService) : IAuthService
{
    private sealed class PendingRegistration
    {
        public required RegisterRequestDTO Request { get; init; }
        public required string OtpHash { get; init; }
        public int Attempts { get; set; }
    }

    private sealed class PendingPasswordReset
    {
        public required string OtpHash { get; init; }
        public int Attempts { get; set; }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequestDTO request)
    {
        var email = NormalizeEmail(request.Email);

        var user = await context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive);

        if (user is null ||
            !PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequestDTO request)
    {
        var user = await CreateMemberAccountAsync(request);
        return CreateAuthResponse(user);
    }

    // Lễ tân tạo tài khoản cho member, nhưng không đăng nhập thay member.
    public async Task<MemberResponseDTO> RegisterAtDeskAsync(
        RegisterRequestDTO request)
    {
        var user = await CreateMemberAccountAsync(request);
        return MemberResponseDTO.FromModel(user.Member!);
    }

    public Task<bool> IsEmailExistingAsync(string email)
    {
        var normalizedEmail = NormalizeEmail(email);

        return context.Users.AnyAsync(
            u => u.Email.ToLower() == normalizedEmail);
    }

    public async Task SendRegisterOtpAsync(RegisterRequestDTO request)
    {
        var email = NormalizeEmail(request.Email);

        if (await IsEmailExistingAsync(email))
        {
            throw new BusinessException(
                StatusCodes.Status409Conflict,
                "Email is already registered.");
        }

        if (request.DateOfBirth?.Date > DateTime.UtcNow.Date)
        {
            throw new BusinessException(
                StatusCodes.Status400BadRequest,
                "Date of birth cannot be in the future.");
        }

        var otp = GenerateOtp();

        cache.Set(
            RegistrationKey(email),
            new PendingRegistration
            {
                Request = request,
                OtpHash = HashOtp(otp)
            },
            TimeSpan.FromMinutes(5));

        await emailService.SendEmailAsync(
            email,
            "Sports Center registration OTP",
            $"Your registration OTP is {otp}. It expires in 5 minutes.");
    }

    public async Task<AuthResponse> VerifyRegisterOtpAsync(
        VerifyRegisterOtpDTO request)
    {
        var email = NormalizeEmail(request.Email);
        var key = RegistrationKey(email);

        if (!cache.TryGetValue(key, out PendingRegistration? pending) ||
            pending is null)
        {
            throw InvalidOtpException();
        }

        if (pending.Attempts >= 5)
        {
            cache.Remove(key);
            throw InvalidOtpException();
        }

        pending.Attempts++;

        if (!MatchesOtp(request.Otp, pending.OtpHash))
        {
            throw InvalidOtpException();
        }

        // Chỉ tạo tài khoản sau khi xác nhận OTP.
        // Hàm này kiểm tra lại email trùng trước khi lưu.
        var user = await CreateMemberAccountAsync(pending.Request);

        cache.Remove(key);
        return CreateAuthResponse(user);
    }

    public async Task RequestResetPasswordOtpAsync(RequestOtpDTO request)
    {
        var email = NormalizeEmail(request.Email);

        var user = await context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive);

        // Không tiết lộ email này có tài khoản hay không.
        if (user is null)
            return;

        var otp = GenerateOtp();

        cache.Set(
            ResetPasswordKey(email),
            new PendingPasswordReset
            {
                OtpHash = HashOtp(otp)
            },
            TimeSpan.FromMinutes(5));

        await emailService.SendEmailAsync(
            email,
            "Sports Center password reset OTP",
            $"Your password reset OTP is {otp}. It expires in 5 minutes.");
    }

    public async Task VerifyResetPasswordOtpAsync(
        VerifyResetPasswordOtpDTO request)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new BusinessException(
                StatusCodes.Status400BadRequest,
                "Passwords do not match.");
        }

        var email = NormalizeEmail(request.Email);
        var key = ResetPasswordKey(email);

        if (!cache.TryGetValue(key, out PendingPasswordReset? pending) ||
            pending is null)
        {
            throw InvalidOtpException();
        }

        if (pending.Attempts >= 5)
        {
            cache.Remove(key);
            throw InvalidOtpException();
        }

        pending.Attempts++;

        if (!MatchesOtp(request.Otp, pending.OtpHash))
        {
            throw InvalidOtpException();
        }

        var user = await context.Users.FirstOrDefaultAsync(
            u => u.Email.ToLower() == email && u.IsActive);

        if (user is null)
            throw InvalidOtpException();

        user.PasswordHash =
            PasswordHelper.HashPassword(request.NewPassword);

        await context.SaveChangesAsync();
        cache.Remove(key);
    }

    private async Task<User> CreateMemberAccountAsync(
        RegisterRequestDTO request)
    {
        var email = NormalizeEmail(request.Email);

        if (await IsEmailExistingAsync(email))
        {
            throw new BusinessException(
                StatusCodes.Status409Conflict,
                "Email is already registered.");
        }

        if (request.DateOfBirth?.Date > DateTime.UtcNow.Date)
        {
            throw new BusinessException(
                StatusCodes.Status400BadRequest,
                "Date of birth cannot be in the future.");
        }

        var now = DateTime.UtcNow;

        var user = new User
        {
            Email = email,
            PasswordHash =
                PasswordHelper.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            PhoneNumber = request.Phone?.Trim(),
            Role = UserRoles.Member,
            IsActive = true,
            CreatedAt = now,
            Member = new Member
            {
                Address = request.Address?.Trim(),
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender?.Trim(),
                FitnessGoal = request.FitnessGoal?.Trim(),
                JoinDate = now
            }
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var jwtSettings = configuration.GetSection("Jwt");

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            Token = JwtHelper.GenerateToken(
                user,
                jwtSettings["SecretKey"]!,
                jwtSettings["Issuer"]!,
                jwtSettings["Audience"]!,
                int.Parse(jwtSettings["ExpireMinutes"] ?? "60"))
        };
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    private static string RegistrationKey(string email) =>
        $"register-otp:{email}";

    private static string ResetPasswordKey(string email) =>
        $"reset-otp:{email}";

    private static string GenerateOtp() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000)
            .ToString("D6");

    private static string HashOtp(string otp) =>
        Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(otp)));

    private static bool MatchesOtp(string otp, string storedHash)
    {
        var actual =
            SHA256.HashData(Encoding.UTF8.GetBytes(otp));

        var expected =
            Convert.FromHexString(storedHash);

        return CryptographicOperations.FixedTimeEquals(
            actual,
            expected);
    }

    private static BusinessException InvalidOtpException() =>
        new(
            StatusCodes.Status400BadRequest,
            "OTP is invalid or expired.");
}