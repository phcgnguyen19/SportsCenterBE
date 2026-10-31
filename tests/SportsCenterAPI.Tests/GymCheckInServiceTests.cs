using SportsCenterAPI.DTOs.CheckIn;
using SportsCenterAPI.Models;
using SportsCenterAPI.Services.Implement;

namespace SportsCenterAPI.Tests;

public class GymCheckInServiceTests
{
    [Fact]
    public async Task GetMyQrCodeAsync_WithActiveSubscription_ReturnsFormattedQrCodeAndActiveStatus()
    {
        using var db = TestDbHelper.CreateDbContext();
        var now = DateTime.UtcNow;

        var user = new User { Id = 10, Email = "mem@gym.com", FullName = "Nguyen Van A", PhoneNumber = "0909111222", Role = "Member", PasswordHash = "h", IsActive = true };
        var member = new Member { Id = 1, UserId = 10, User = user };
        var package = new MembershipPackage { Id = 1, PackageName = "Gold 6 Tháng", Price = 4500000m, DurationInDays = 180, IsActive = true };
        var sub = new MemberSubscription
        {
            Id = 100,
            MemberId = 1,
            PackageId = 1,
            Package = package,
            Status = "Active",
            StartDate = now.AddDays(-5),
            EndDate = now.AddDays(175)
        };

        db.Users.Add(user);
        db.Members.Add(member);
        db.MembershipPackages.Add(package);
        db.MemberSubscriptions.Add(sub);
        await db.SaveChangesAsync();

        var service = new GymCheckInService(db);

        var qr = await service.GetMyQrCodeAsync(10);

        Assert.NotNull(qr);
        Assert.Equal("SC-MEM-000001", qr.MemberCode);
        Assert.Equal("SC-MEM-000001", qr.QrData);
        Assert.Equal("Active", qr.PackageStatus);
        Assert.Equal("Gold 6 Tháng", qr.PackageName);
    }

    [Fact]
    public async Task ScanQrCheckInAsync_ValidFormattedQrCode_ParsesMemberIdAndChecksIn()
    {
        using var db = TestDbHelper.CreateDbContext();
        var now = DateTime.UtcNow;

        var staff = new User { Id = 99, Email = "staff@gym.com", FullName = "Lễ Tân A", Role = "Receptionist", PasswordHash = "h", IsActive = true };
        var user = new User { Id = 50, Email = "mem5@gym.com", FullName = "Hội Viên 5", PhoneNumber = "0905555555", Role = "Member", PasswordHash = "h", IsActive = true };
        var member = new Member { Id = 5, UserId = 50, User = user };
        var package = new MembershipPackage { Id = 1, PackageName = "Flex Pass", Price = 1000000m, DurationInDays = 30, IsActive = true };
        var sub = new MemberSubscription
        {
            Id = 101,
            MemberId = 5,
            PackageId = 1,
            Package = package,
            Status = "Active",
            StartDate = now.AddDays(-2),
            EndDate = now.AddDays(28)
        };

        db.Users.AddRange(staff, user);
        db.Members.Add(member);
        db.MembershipPackages.Add(package);
        db.MemberSubscriptions.Add(sub);
        await db.SaveChangesAsync();

        var service = new GymCheckInService(db);

        var result = await service.ScanQrCheckInAsync(99, new ScanQrRequest { QrCode = "SC-MEM-000005" });

        Assert.NotNull(result);
        Assert.Equal(5, result.MemberId);
        Assert.Equal("SC-MEM-000005", result.MemberCode);
        Assert.Equal("Lễ Tân A", result.CheckedByStaffName);
        Assert.Null(result.CheckOutTime);
    }

    [Fact]
    public async Task CheckInMemberAsync_ByPhoneNumber_FindsMemberAndCreatesCheckIn()
    {
        using var db = TestDbHelper.CreateDbContext();
        var now = DateTime.UtcNow;

        var staff = new User { Id = 99, Email = "staff@gym.com", FullName = "Lễ Tân", Role = "Receptionist", PasswordHash = "h", IsActive = true };
        var user = new User { Id = 60, Email = "mem6@gym.com", FullName = "Trần Thị B", PhoneNumber = "0988777666", Role = "Member", PasswordHash = "h", IsActive = true };
        var member = new Member { Id = 6, UserId = 60, User = user };
        user.Member = member;

        var package = new MembershipPackage { Id = 1, PackageName = "VIP Club", Price = 12000000m, DurationInDays = 365, IsActive = true };
        var sub = new MemberSubscription
        {
            Id = 102,
            MemberId = 6,
            PackageId = 1,
            Package = package,
            Status = "Active",
            StartDate = now.AddDays(-10),
            EndDate = now.AddDays(355)
        };

        db.Users.AddRange(staff, user);
        db.Members.Add(member);
        db.MembershipPackages.Add(package);
        db.MemberSubscriptions.Add(sub);
        await db.SaveChangesAsync();

        var service = new GymCheckInService(db);

        var result = await service.CheckInMemberAsync(99, new CheckInRequest { PhoneNumber = "0988777666", Notes = "Quên mang thẻ" });

        Assert.NotNull(result);
        Assert.Equal(6, result.MemberId);
        Assert.Equal("Trần Thị B", result.MemberName);
        Assert.Equal("VIP Club", result.PackageName);
    }

    [Fact]
    public async Task CheckInMemberAsync_ExpiredSubscription_ThrowsInvalidOperationException()
    {
        using var db = TestDbHelper.CreateDbContext();
        var now = DateTime.UtcNow;

        var user = new User { Id = 70, Email = "expired@gym.com", FullName = "Hết Hạn", Role = "Member", PasswordHash = "h", IsActive = true };
        var member = new Member { Id = 7, UserId = 70, User = user };
        var package = new MembershipPackage { Id = 1, PackageName = "Gói 1 Tháng", Price = 1000000m, DurationInDays = 30, IsActive = true };
        var expiredSub = new MemberSubscription
        {
            Id = 103,
            MemberId = 7,
            PackageId = 1,
            Package = package,
            Status = "Active",
            StartDate = now.AddDays(-40),
            EndDate = now.AddDays(-10) // Đã hết hạn 10 ngày trước
        };

        db.Users.Add(user);
        db.Members.Add(member);
        db.MembershipPackages.Add(package);
        db.MemberSubscriptions.Add(expiredSub);
        await db.SaveChangesAsync();

        var service = new GymCheckInService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CheckInMemberAsync(99, new CheckInRequest { MemberId = 7 }));
    }

    [Fact]
    public async Task CheckInMemberAsync_AlreadyCheckedInWithoutCheckOutToday_ThrowsInvalidOperationException()
    {
        using var db = TestDbHelper.CreateDbContext();
        var now = DateTime.UtcNow;

        var user = new User { Id = 80, Email = "dup@gym.com", FullName = "Hội Viên Đang Tập", Role = "Member", PasswordHash = "h", IsActive = true };
        var member = new Member { Id = 8, UserId = 80, User = user };
        var package = new MembershipPackage { Id = 1, PackageName = "Gói 1 Tháng", Price = 1000000m, DurationInDays = 30, IsActive = true };
        var activeSub = new MemberSubscription
        {
            Id = 104,
            MemberId = 8,
            PackageId = 1,
            Package = package,
            Status = "Active",
            StartDate = now.AddDays(-5),
            EndDate = now.AddDays(25)
        };

        db.Users.Add(user);
        db.Members.Add(member);
        db.MembershipPackages.Add(package);
        db.MemberSubscriptions.Add(activeSub);
        db.GymCheckIns.Add(new GymCheckIn
        {
            Id = 500,
            MemberId = 8,
            SubscriptionId = 104,
            CheckInTime = now.AddMinutes(-30),
            CheckOutTime = null // Chưa check-out
        });
        await db.SaveChangesAsync();

        var service = new GymCheckInService(db);

        // Quẹt thẻ lần 2 khi chưa check-out -> Phải chặn
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CheckInMemberAsync(99, new CheckInRequest { MemberId = 8 }));
    }

    [Fact]
    public async Task CheckOutMemberAsync_OpenCheckIn_SetsCheckOutTime_AndBlocksDuplicateCheckOut()
    {
        using var db = TestDbHelper.CreateDbContext();
        var now = DateTime.UtcNow;

        var user = new User { Id = 90, Email = "out@gym.com", FullName = "Hội Viên Ra Về", Role = "Member", PasswordHash = "h", IsActive = true };
        var member = new Member { Id = 9, UserId = 90, User = user };
        var package = new MembershipPackage { Id = 1, PackageName = "Gói 1 Tháng", Price = 1000000m, DurationInDays = 30, IsActive = true };
        var activeSub = new MemberSubscription
        {
            Id = 105,
            MemberId = 9,
            PackageId = 1,
            Package = package,
            Status = "Active",
            StartDate = now.AddDays(-5),
            EndDate = now.AddDays(25)
        };
        var checkIn = new GymCheckIn
        {
            Id = 501,
            MemberId = 9,
            Member = member,
            SubscriptionId = 105,
            Subscription = activeSub,
            CheckInTime = now.AddHours(-1),
            CheckOutTime = null
        };

        db.Users.Add(user);
        db.Members.Add(member);
        db.MembershipPackages.Add(package);
        db.MemberSubscriptions.Add(activeSub);
        db.GymCheckIns.Add(checkIn);
        await db.SaveChangesAsync();

        var service = new GymCheckInService(db);

        // 1. Check-out lần 1 -> Thành công
        var result = await service.CheckOutMemberAsync(501);
        Assert.NotNull(result.CheckOutTime);

        // 2. Check-out lần 2 cho cùng lượt đó -> Phải chặn
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CheckOutMemberAsync(501));
    }
}