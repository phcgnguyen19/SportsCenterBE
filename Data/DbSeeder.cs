using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace SportsCenterAPI.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Nếu đã có dữ liệu mẫu (ngoài Admin) thì không seed lại
        if (await db.Members.AnyAsync()) return;

        // ===== 1. TẠO TÀI KHOẢN NHÂN VIÊN =====
        var now = DateTime.UtcNow;
        var passwordHash = PasswordHelper.HashPassword("Password@123");

        var receptionist = new User
        {
            FullName = "Trần Thị Lễ Tân",
            Email = "receptionist@sportscenter.com",
            PasswordHash = passwordHash,
            Role = UserRoles.Receptionist,
            PhoneNumber = "0912345678",
            IsActive = true,
            CreatedAt = now
        };

        var coachUser1 = new User
        {
            FullName = "Nguyễn Văn HLV",
            Email = "coach1@sportscenter.com",
            PasswordHash = passwordHash,
            Role = UserRoles.Coach,
            PhoneNumber = "0923456789",
            IsActive = true,
            CreatedAt = now,
            Coach = new Coach
            {
                Specialization = "Yoga, Pilates",
                Bio = "10 năm kinh nghiệm huấn luyện Yoga chuyên nghiệp",
                YearsOfExperience = 10
            }
        };

        // ===== 2. TẠO TÀI KHOẢN HỘI VIÊN =====
        var member1 = new User
        {
            FullName = "Phạm Văn An",
            Email = "member1@gmail.com",
            PasswordHash = passwordHash,
            Role = UserRoles.Member,
            PhoneNumber = "0945678901",
            IsActive = true,
            CreatedAt = now,
            Member = new Member
            {
                DateOfBirth = new DateTime(1995, 5, 15),
                Gender = "Nam",
                Address = "123 Nguyễn Huệ, Q1, TP.HCM",
                FitnessGoal = "Giảm cân, tăng cơ",
                JoinDate = now
            }
        };

        db.Users.AddRange(receptionist, coachUser1, member1);
        await db.SaveChangesAsync();

        // ===== 3. TẠO GÓI TẬP & BỘ MÔN =====
        var basicPkg = new MembershipPackage
        {
            PackageName = "Gói VIP 1 Tháng",
            Description = "Tập gym full thiết bị",
            Price = 1_000_000m,
            DurationInDays = 30,
            IsActive = true
        };
        db.MembershipPackages.Add(basicPkg);

        var yogaSport = new Sport { Name = "Yoga", Description = "Yoga giảm stress", IsActive = true };
        db.Sports.Add(yogaSport);
        await db.SaveChangesAsync();
    }
}