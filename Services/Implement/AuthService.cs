using Microsoft.EntityFrameworkCore;
using SportsCenterAPI.Data;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models;
using SportsCenterAPI.Models.DTOs.Accounts;
using SportsCenterAPI.Models.DTOs.Auth;
using SportsCenterAPI.Models.DTOs.Login;
using SportsCenterAPI.Models.DTOs.Register;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Services.Implement;

public class AuthService(AppDbContext context, IConfiguration configuration) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequestDTO request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive);

        if (user == null || !PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequestDTO request)
    {
        var user = await CreateMemberAccountAsync(request);
        return CreateAuthResponse(user);
    }

    // Front-desk registration returns the profile without signing the employee in as that member.
    public async Task<MemberResponseDTO> RegisterAtDeskAsync(RegisterRequestDTO request)
    {
        var user = await CreateMemberAccountAsync(request);
        return MemberResponseDTO.FromModel(user.Member!);
    }

    private async Task<User> CreateMemberAccountAsync(RegisterRequestDTO request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await IsEmailExistingAsync(email))
            throw new BusinessException(StatusCodes.Status409Conflict, "Email is already registered.");

        if (request.DateOfBirth?.Date > DateTime.UtcNow.Date)
            throw new BusinessException(StatusCodes.Status400BadRequest, "Date of birth cannot be in the future.");

        var now = DateTime.UtcNow;
        var user = new User
        {
            Email = email,
            PasswordHash = PasswordHelper.HashPassword(request.Password),
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

        // One SaveChanges transaction: a failed Member insert cannot leave an orphan User.
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
            Token = JwtHelper.GenerateToken(user, jwtSettings["SecretKey"]!,
                jwtSettings["Issuer"]!, jwtSettings["Audience"]!,
                int.Parse(jwtSettings["ExpireMinutes"] ?? "60"))
        };
    }

    public Task<bool> IsEmailExistingAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
    }
}
