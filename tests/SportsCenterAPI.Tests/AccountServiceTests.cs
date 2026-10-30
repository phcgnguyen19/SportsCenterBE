using Microsoft.EntityFrameworkCore;
using SportsCenterAPI.DTOs.Accounts;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models;
using SportsCenterAPI.Services.Implement;

namespace SportsCenterAPI.Tests;

public class AccountServiceTests
{
    private static (AccountService service, Data.AppDbContext db) CreateService()
    {
        var db = TestDbHelper.CreateDbContext();
        var service = new AccountService(db);
        return (service, db);
    }

    [Fact]
    public async Task CreateStaff_ValidCoachRole_CreatesUserCoachAndAuditLog()
    {
        // Arrange
        var (service, db) = CreateService();

        var manager = new User
        {
            Id = 1,
            FullName = "Admin Manager",
            Email = "manager@sport.com",
            Role = UserRoles.Manager,
            IsActive = true
        };
        db.Users.Add(manager);
        await db.SaveChangesAsync();

        var request = new CreateStaffRequestDTO
        {
            Email = "newcoach@sport.com",
            FullName = "Nguyễn Văn HLV",
            Password = "Password@123",
            Phone = "0909123456",
            Role = UserRoles.Coach
        };

        // Act
        var result = await service.CreateStaffAsync(request, callerUserId: 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newcoach@sport.com", result.Email);
        Assert.Equal(UserRoles.Coach, result.Role);

        var createdUser = await db.Users
            .Include(u => u.Coach)
            .SingleOrDefaultAsync(u => u.Email == "newcoach@sport.com");

        Assert.NotNull(createdUser);
        Assert.NotNull(createdUser.Coach); // Phải tự động tạo bản ghi Coach đi kèm

        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.EntityId == createdUser.Id && a.Action == "Create");
        Assert.NotNull(audit);
    }

    [Fact]
    public async Task CreateStaff_NonManagerCaller_ThrowsForbidden403()
    {
        // Arrange
        var (service, db) = CreateService();

        var receptionist = new User
        {
            Id = 2,
            FullName = "Lễ Tân",
            Email = "letan@sport.com",
            Role = UserRoles.Receptionist,
            IsActive = true
        };
        db.Users.Add(receptionist);
        await db.SaveChangesAsync();

        var request = new CreateStaffRequestDTO
        {
            Email = "staff@sport.com",
            FullName = "Staff Mới",
            Password = "Password@123",
            Role = UserRoles.Receptionist
        };

        // Act & Assert (Lễ tân không có quyền tạo nhân viên)
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CreateStaffAsync(request, callerUserId: 2));

        Assert.Equal(403, ex.StatusCode);
        Assert.Contains("Chỉ quản lý đang hoạt động", ex.Message);
    }

    [Fact]
    public async Task CreateStaff_DuplicateEmail_ThrowsConflict409()
    {
        // Arrange
        var (service, db) = CreateService();

        var manager = new User { Id = 1, FullName = "Manager", Email = "manager@sport.com", Role = UserRoles.Manager, IsActive = true };
        var existingUser = new User { Id = 2, FullName = "Existing", Email = "staff@sport.com", Role = UserRoles.Receptionist, IsActive = true };

        db.Users.AddRange(manager, existingUser);
        await db.SaveChangesAsync();

        var request = new CreateStaffRequestDTO
        {
            Email = "STAFF@sport.com", // Test cả viết hoa
            FullName = "Duplicate Staff",
            Password = "Password@123",
            Role = UserRoles.Coach
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CreateStaffAsync(request, callerUserId: 1));

        Assert.Equal(409, ex.StatusCode);
        Assert.Contains("Email đã được đăng ký", ex.Message);
    }

    [Fact]
    public async Task UpdateStaffRole_ManagerSelfChange_ThrowsConflict409()
    {
        // Arrange
        var (service, db) = CreateService();

        var manager = new User
        {
            Id = 1,
            FullName = "Manager",
            Email = "manager@sport.com",
            Role = UserRoles.Manager,
            IsActive = true
        };
        db.Users.Add(manager);
        await db.SaveChangesAsync();

        var request = new UpdateStaffRoleRequestDTO { Role = UserRoles.Receptionist };

        // Act & Assert (Quản lý không được tự hạ quyền chính mình)
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.UpdateStaffRoleAsync(userId: 1, request, callerUserId: 1));

        Assert.Equal(409, ex.StatusCode);
        Assert.Contains("không thể tự đổi quyền của mình", ex.Message);
    }

    [Fact]
    public async Task DeactivateStaff_SelfDeactivate_ThrowsConflict409()
    {
        // Arrange
        var (service, db) = CreateService();

        var manager = new User
        {
            Id = 1,
            FullName = "Manager",
            Email = "manager@sport.com",
            Role = UserRoles.Manager,
            IsActive = true
        };
        db.Users.Add(manager);
        await db.SaveChangesAsync();

        // Act & Assert (Không được tự khóa tài khoản của chính mình)
        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.DeactivateStaffAsync(userId: 1, callerUserId: 1));

        Assert.Equal(409, ex.StatusCode);
        Assert.Contains("không thể vô hiệu hóa tài khoản của mình", ex.Message);
    }

    [Fact]
    public async Task DeactivateMember_ValidMember_SoftDeletesUserAndLogsAudit()
    {
        // Arrange
        var (service, db) = CreateService();

        var manager = new User { Id = 1, FullName = "Manager", Email = "manager@sport.com", Role = UserRoles.Manager, IsActive = true };
        var memberUser = new User { Id = 20, FullName = "Hội Viên A", Email = "membera@sport.com", Role = UserRoles.Member, IsActive = true };
        var member = new Member { Id = 10, UserId = 20, User = memberUser };

        db.Users.AddRange(manager, memberUser);
        db.Members.Add(member);
        await db.SaveChangesAsync();

        // Act
        await service.DeactivateMemberAsync(memberId: 10, callerUserId: 1);

        // Assert
        var updatedUser = await db.Users.FindAsync(20);
        Assert.NotNull(updatedUser);
        Assert.False(updatedUser.IsActive); // Đã bị vô hiệu hóa (Soft Delete)

        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.EntityName == nameof(Member) && a.EntityId == 10 && a.Action == "Delete");
        Assert.NotNull(audit);
    }
}