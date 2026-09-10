using Microsoft.EntityFrameworkCore;
using SportsCenterAPI.Data;
using SportsCenterAPI.DTOs.Auth;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Services.Implement;

/// <summary>
/// Service xử lý logic xác thực: đăng nhập, đăng ký
/// Authentication service: handles login and registration logic
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// Đăng nhập — kiểm tra email + password, trả về JWT token
    /// Login — verify email + password, return JWT token
    /// </summary>
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Tìm user theo email / Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        // Kiểm tra mật khẩu / Verify password
        if (!PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        // Tạo JWT token / Generate JWT token
        var token = GenerateJwtToken(user);

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            Token = token
        };
    }

    /// <summary>
    /// Đăng ký thành viên mới — tạo User + Member, trả về JWT token
    /// Register new member — create User + Member records, return JWT token
    /// </summary>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Kiểm tra email đã tồn tại chưa / Check if email already exists
        var existingUser = await _context.Users
            .AnyAsync(u => u.Email == request.Email);

        if (existingUser)
            throw new InvalidOperationException("Email is already registered.");

        // Tạo User mới / Create new User
        var user = new User
        {
            Email = request.Email,
            PasswordHash = PasswordHelper.HashPassword(request.Password),
            FullName = request.FullName,
            PhoneNumber = request.Phone,
            Role = "Member",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Tạo Member profile / Create Member profile
        var member = new Member
        {
            UserId = user.Id,
            Address = request.Address,
            DateOfBirth = request.DateOfBirth
        };

        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        // Tạo JWT token / Generate JWT token
        var token = GenerateJwtToken(user);

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            Token = token
        };
    }

    /// <summary>
    /// Helper: tạo JWT token từ thông tin user
    /// </summary>
    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        return JwtHelper.GenerateToken(
            user,
            jwtSettings["SecretKey"]!,
            jwtSettings["Issuer"]!,
            jwtSettings["Audience"]!,
            int.Parse(jwtSettings["ExpireMinutes"] ?? "60")
        );
    }
}
