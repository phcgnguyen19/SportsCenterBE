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
                DateOfBirth = new DateTime(1995, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Nam",
                Address = "123 Nguyễn Huệ, Q1, TP.HCM",
                FitnessGoal = "Giảm cân, tăng cơ",
                JoinDate = now
            }
        };

        db.Users.AddRange(receptionist, coachUser1, member1);
        await db.SaveChangesAsync();

        // ===== 3. TẠO 4 GÓI TẬP (GIỐNG FRONTEND) & BỘ MÔN =====
        var packages = new List<MembershipPackage>
        {
            new MembershipPackage
            {
                PackageName = "Flex Pass - Tự Do",
                Description = "Dành cho người thích linh động, tập buổi nào trừ tiền buổi đó.",
                DurationInDays = 30, // 1 tháng
                Price = 800000m,
                IsActive = true
            },
            new MembershipPackage
            {
                PackageName = "Gold All-Access",
                Description = "Trải nghiệm đỉnh cao với quyền ưu tiên đặt giờ vàng, tích hợp xông hơi.",
                DurationInDays = 180, // 6 tháng
                Price = 4500000m,
                IsActive = true
            },
            new MembershipPackage
            {
                PackageName = "Olympus VIP Club",
                Description = "Đẳng cấp doanh nhân & gia đình, đặc quyền không giới hạn.",
                DurationInDays = 365, // 1 năm
                Price = 12000000m,
                IsActive = true
            },
            new MembershipPackage
            {
                PackageName = "PT Pro Training 1-1",
                Description = "Luyện tập cá nhân hóa trực tiếp với huấn luyện viên chuyên nghiệp.",
                DurationInDays = 90, // 3 tháng
                Price = 6000000m,
                IsActive = true
            }
        };
        db.MembershipPackages.AddRange(packages);

        var yogaSport = new Sport { Name = "Yoga", Description = "Yoga giảm stress", IsActive = true };
        db.Sports.Add(yogaSport);
        await db.SaveChangesAsync();

        // ===== 4. GIẢ LẬP HỘI VIÊN MUA GÓI ĐỂ HIỆN LÊN GIAO DIỆN =====
        var goldPkg = packages.FirstOrDefault(p => p.PackageName == "Gold All-Access");
        if (goldPkg != null)
        {
            var dummySubscription = new MemberSubscription
            {
                MemberId = member1.Member.Id,
                PackageId = goldPkg.Id,
                StartDate = now,
                EndDate = now.AddDays(goldPkg.DurationInDays),
                AgreedPrice = goldPkg.Price,
                Status = "Active"
            };
            db.MemberSubscriptions.Add(dummySubscription);
            await db.SaveChangesAsync();
        }
    }
}