using Microsoft.EntityFrameworkCore;
using SportsCenterAPI.Data;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models;
using SportsCenterAPI.Models.DTOs.Accounts;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Services.Implement;

public class AccountService(AppDbContext context) : IAccountService
{
    public async Task<IReadOnlyList<MemberResponseDTO>> GetMembersAsync()
    {
        var members = await context.Members.AsNoTracking().Include(m => m.User)
            .OrderByDescending(m => m.Id).ToListAsync();
        return members.Select(MemberResponseDTO.FromModel).ToList();
    }

    public async Task<MemberResponseDTO> GetMemberAsync(int memberId)
    {
        var member = await context.Members.AsNoTracking().Include(m => m.User)
            .SingleOrDefaultAsync(m => m.Id == memberId)
            ?? throw new BusinessException(StatusCodes.Status404NotFound, "Member not found.");
        return MemberResponseDTO.FromModel(member);
    }

    public async Task<MemberResponseDTO> GetMyMemberAsync(int userId)
    {
        var member = await context.Members.AsNoTracking().Include(m => m.User)
            .SingleOrDefaultAsync(m => m.UserId == userId)
            ?? throw new BusinessException(StatusCodes.Status404NotFound, "Member profile not found.");
        return MemberResponseDTO.FromModel(member);
    }

    public async Task DeactivateMemberAsync(int memberId)
    {
        var member = await context.Members.Include(m => m.User)
            .SingleOrDefaultAsync(m => m.Id == memberId)
            ?? throw new BusinessException(StatusCodes.Status404NotFound, "Member not found.");
        if (!member.User.IsActive)
            return;

        member.User.IsActive = false;
        member.User.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<StaffResponseDTO>> GetStaffAsync()
    {
        var staff = await StaffQuery().AsNoTracking().OrderBy(u => u.Id).ToListAsync();
        return staff.Select(StaffResponseDTO.FromModel).ToList();
    }

    public async Task<StaffResponseDTO> GetStaffAsync(int userId)
    {
        var user = await StaffQuery().AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId)
            ?? throw new BusinessException(StatusCodes.Status404NotFound, "Staff account not found.");
        return StaffResponseDTO.FromModel(user);
    }

    public async Task<StaffResponseDTO> CreateStaffAsync(CreateStaffRequestDTO request)
    {
        ValidateStaffRole(request.Role);
        var email = request.Email.Trim().ToLowerInvariant();
        if (await context.Users.AnyAsync(u => u.Email.ToLower() == email))
            throw new BusinessException(StatusCodes.Status409Conflict, "Email is already registered.");

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            PhoneNumber = request.Phone?.Trim(),
            PasswordHash = PasswordHelper.HashPassword(request.Password),
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Coach = request.Role == UserRoles.Coach ? new Coach() : null
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return StaffResponseDTO.FromModel(user);
    }

    public async Task<StaffResponseDTO> UpdateStaffRoleAsync(
        int userId, UpdateStaffRoleRequestDTO request, int callerUserId)
    {
        ValidateStaffRole(request.Role);
        var user = await StaffQuery().SingleOrDefaultAsync(u => u.Id == userId)
            ?? throw new BusinessException(StatusCodes.Status404NotFound, "Staff account not found.");

        if (user.Role == request.Role)
            return StaffResponseDTO.FromModel(user);

        if (userId == callerUserId)
            throw new BusinessException(StatusCodes.Status409Conflict, "You cannot change your own staff role.");

        user.Role = request.Role;
        user.UpdatedAt = DateTime.UtcNow;
        if (request.Role == UserRoles.Coach && user.Coach == null)
            user.Coach = new Coach();

        // Retain existing Coach records on role changes so historical classes/plans keep their links.
        await context.SaveChangesAsync();
        return StaffResponseDTO.FromModel(user);
    }

    public async Task DeactivateStaffAsync(int userId, int callerUserId)
    {
        if (userId == callerUserId)
            throw new BusinessException(StatusCodes.Status409Conflict, "You cannot deactivate your own account.");

        var user = await StaffQuery().SingleOrDefaultAsync(u => u.Id == userId)
            ?? throw new BusinessException(StatusCodes.Status404NotFound, "Staff account not found.");
        if (!user.IsActive)
            return;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    private IQueryable<User> StaffQuery() => context.Users.Include(u => u.Coach)
        .Where(u => u.Member == null &&
            (u.Role == UserRoles.Manager || u.Role == UserRoles.Receptionist || u.Role == UserRoles.Coach));

    private static void ValidateStaffRole(string role)
    {
        if (!UserRoles.IsStaff(role))
            throw new BusinessException(StatusCodes.Status400BadRequest,
                "Staff role must be Manager, Receptionist or Coach.");
    }
}
