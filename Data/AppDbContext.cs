using Microsoft.EntityFrameworkCore;
using SportsCenterAPI.Models;

namespace SportsCenterAPI.Data
{
    /// <summary>
    /// Entity Framework Core database context for the Sports Center Management System.
    /// Manages database connection and entity configurations, including class sessions and reviews.
    /// 
    /// DbContext cho hệ thống quản lý trung tâm thể thao.
    /// Quản lý kết nối cơ sở dữ liệu và cấu hình các thực thể trong hệ thống.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        #region DbSets
        /// <summary>
        /// Accounts / Tài khoản người dùng (Admin, Manager, Coach, Member)
        /// </summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>
        /// Member profiles / Hồ sơ hội viên
        /// </summary>
        public DbSet<Member> Members { get; set; } = null!;

        /// <summary>
        /// Coach profiles / Hồ sơ huấn luyện viên
        /// </summary>
        public DbSet<Coach> Coaches { get; set; } = null!;

        /// <summary>
        /// Sport disciplines / Bộ môn thể thao (Gym, Yoga, Bơi lội, Boxing, v.v.)
        /// </summary>
        public DbSet<Sport> Sports { get; set; } = null!;

        /// <summary>
        /// Sport classes / Lớp học thể thao
        /// </summary>
        public DbSet<SportClass> SportClasses { get; set; } = null!;

        /// <summary>
        /// Membership packages / Gói tập hội viên (Theo tháng, theo năm, VIP)
        /// </summary>
        public DbSet<MembershipPackage> MembershipPackages { get; set; } = null!;

        /// <summary>
        /// Member subscription records / Đăng ký gói tập của hội viên
        /// </summary>
        public DbSet<MemberSubscription> MemberSubscriptions { get; set; } = null!;

        /// <summary>
        /// Class registrations / Đăng ký tham gia lớp học
        /// </summary>
        public DbSet<ClassRegistration> ClassRegistrations { get; set; } = null!;

        /// <summary>
        /// Attendance tracking / Điểm danh lớp học
        /// </summary>
        public DbSet<Attendance> Attendances { get; set; } = null!;

        /// <summary>
        /// Payment transactions / Giao dịch thanh toán
        /// </summary>
        public DbSet<Payment> Payments { get; set; } = null!;

        /// <summary>
        /// Personalized training plans / Lộ trình tập luyện cá nhân
        /// </summary>
        public DbSet<TrainingPlan> TrainingPlans { get; set; } = null!;

        /// <summary>
        /// Exercise catalog / Danh mục bài tập
        /// </summary>
        public DbSet<Exercise> Exercises { get; set; } = null!;

        /// <summary>
        /// Mapping exercises to training plans / Bài tập trong kế hoạch tập luyện
        /// </summary>
        public DbSet<TrainingPlanExercise> TrainingPlanExercises { get; set; } = null!;

        /// <summary>
        /// System notifications / Thông báo hệ thống
        /// </summary>
        public DbSet<Notification> Notifications { get; set; } = null!;

        /// <summary>
        /// System audit logs / Nhật ký kiểm toán thao tác hệ thống
        /// </summary>
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<ClassSession> ClassSessions { get; set; } = null!;
        public DbSet<CancellationPolicy> CancellationPolicies { get; set; } = null!;
        public DbSet<ClassReview> ClassReviews { get; set; } = null!;
        #endregion

        /// <summary>
        /// Configures entity relationships, indexes, constraints, decimal precisions, and initial seed data.
        /// Cấu hình mối quan hệ giữa các bảng, chỉ mục, ràng buộc, độ chính xác số thập phân và dữ liệu ban đầu.
        /// </summary>
        /// <param name="modelBuilder">EF Core ModelBuilder instance</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            #region 1. Unique Indexes / Chỉ mục duy nhất
            // Ensure User Email is unique across the system
            // Đảm bảo Email người dùng là duy nhất trong toàn hệ thống
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Ensure 1-to-1 relationship between User and Member: Member.UserId is unique
            // Đảm bảo quan hệ 1-1: Mỗi User chỉ liên kết với tối đa một Member profile
            modelBuilder.Entity<Member>()
                .HasIndex(m => m.UserId)
                .IsUnique();

            // Ensure 1-to-1 relationship between User and Coach: Coach.UserId is unique
            // Đảm bảo quan hệ 1-1: Mỗi User chỉ liên kết với tối đa một Coach profile
            modelBuilder.Entity<Coach>()
                .HasIndex(c => c.UserId)
                .IsUnique();

            // Preserve the old uniqueness rule only for legacy class-level records.
            // New bookings are unique per member/session while not cancelled.
            modelBuilder.Entity<ClassRegistration>()
                .HasIndex(cr => new { cr.MemberId, cr.ClassId })
                .IsUnique().HasFilter("[SessionId] IS NULL");
            modelBuilder.Entity<ClassRegistration>()
                .HasIndex(cr => new { cr.MemberId, cr.SessionId })
                .IsUnique().HasFilter("[SessionId] IS NOT NULL AND [Status] <> N'Cancelled'");
            modelBuilder.Entity<ClassRegistration>().Property(r => r.Status).HasMaxLength(20);
            modelBuilder.Entity<ClassSession>().HasAlternateKey(s => new { s.Id, s.ClassId });
            modelBuilder.Entity<ClassRegistration>().HasOne(r => r.Session).WithMany(s => s.Registrations)
                .HasForeignKey(r => new { r.SessionId, r.ClassId }).HasPrincipalKey(s => new { s.Id, s.ClassId });
            modelBuilder.Entity<Attendance>().HasOne(a => a.Session).WithMany(s => s.Attendances)
                .HasForeignKey(a => new { a.SessionId, a.ClassId }).HasPrincipalKey(s => new { s.Id, s.ClassId });
            modelBuilder.Entity<Attendance>().HasIndex(a => new { a.MemberId, a.SessionId })
                .IsUnique().HasFilter("[SessionId] IS NOT NULL");
            modelBuilder.Entity<ClassRegistration>().HasOne(r => r.CreatedByUser).WithMany().HasForeignKey(r => r.CreatedByUserId);
            modelBuilder.Entity<ClassRegistration>().HasOne(r => r.CancelledByUser).WithMany().HasForeignKey(r => r.CancelledByUserId);
            modelBuilder.Entity<ClassReview>().HasIndex(r => r.RegistrationId).IsUnique();
            modelBuilder.Entity<ClassReview>().ToTable(t => t.HasCheckConstraint("CK_ClassReviews_Rating", "[Rating] BETWEEN 1 AND 5"));
            modelBuilder.Entity<ClassSession>().HasIndex(s => new { s.CoachId, s.StartsAt, s.EndsAt });
            modelBuilder.Entity<ClassSession>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_ClassSessions_Time", "[EndsAt] > [StartsAt]");
                t.HasCheckConstraint("CK_ClassSessions_Capacity", "[Capacity] > 0");
            });
            modelBuilder.Entity<CancellationPolicy>().ToTable(t => t.HasCheckConstraint("CK_CancellationPolicies_Hours", "[MinimumHoursBeforeStart] BETWEEN 2 AND 720"));
            modelBuilder.Entity<CancellationPolicy>().HasData(new CancellationPolicy
            {
                Id = 1, Name = "Hủy trước ít nhất 2 tiếng", MinimumHoursBeforeStart = 2, IsActive = true
            });

            modelBuilder.Entity<MemberSubscription>()
                .HasIndex(subscription => new { subscription.MemberId, subscription.PackageId, subscription.Status });
            modelBuilder.Entity<MemberSubscription>()
                .Property(subscription => subscription.Status).HasMaxLength(20);
            modelBuilder.Entity<Payment>()
                .HasIndex(payment => payment.SubscriptionId).IsUnique()
                .HasFilter("[SubscriptionId] IS NOT NULL AND [Status] = N'Completed'");
            modelBuilder.Entity<Payment>()
                .Property(payment => payment.TransactionReference).HasMaxLength(100);
            modelBuilder.Entity<Payment>()
                .HasIndex(payment => payment.TransactionReference).IsUnique()
                .HasFilter("[TransactionReference] IS NOT NULL");
            #endregion

            #region 2. Decimal Precision Configurations / Cấu hình độ chính xác số tiền
            // Payment Amount with 18 digits precision and 2 decimal places
            // Số tiền thanh toán với độ chính xác 18 chữ số và 2 chữ số thập phân
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            // MembershipPackage Price precision (18, 2)
            modelBuilder.Entity<MembershipPackage>()
                .Property(mp => mp.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<MemberSubscription>()
                .Property(subscription => subscription.AgreedPrice).HasPrecision(18, 2);

            // SportClass Price precision (18, 2)
            modelBuilder.Entity<SportClass>()
                .Property(sc => sc.Price)
                .HasPrecision(18, 2);
            #endregion

            #region 3. Cascade Delete Behavior / Hành vi xóa tầng
            // Set global cascade delete behavior to Restrict across all foreign key relationships
            // This prevents "may cause cycles or multiple cascade paths" errors in SQL Server.
            //
            // Đặt hành vi xóa khóa ngoại toàn cục thành Restrict để ngăn ngừa vòng lặp xóa tầng
            // và lỗi xung đột khóa ngoại trong SQL Server.
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
            #endregion

            #region 4. Data Seeding / Khởi tạo dữ liệu mặc định
            // Seed a default administrator (Manager role)
            // Email: admin@sportscenter.com
            // Plain Password: Admin@123
            //
            // Khởi tạo tài khoản quản trị mặc định (vai trò Manager).
            // Sử dụng chuỗi băm BCrypt cố định để đảm bảo migrations của EF Core không bị thay đổi ngẫu nhiên mỗi lần chạy.
            const string defaultAdminPasswordHash = "$2a$11$yyFr5UfBjj8AJQZXXDiuUuNkYvAYEwcdV7EYeGOdE4CsHPDimmLt6"; // BCrypt hash for "Admin@123"

            modelBuilder.Entity<User>().HasData(new User
            {
                Id = 1,
                FullName = "System Administrator",
                Email = "admin@sportscenter.com",
                PasswordHash = defaultAdminPasswordHash,
                Role = "Manager",
                PhoneNumber = "0901234567",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            #endregion
        }
    }
}
