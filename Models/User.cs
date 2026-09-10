namespace SportsCenterAPI.Models
{
    /// <summary>
    /// System user account entity (Admin, Manager, Coach, Member).
    /// Thực thể tài khoản người dùng hệ thống.
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Member"; // Manager, Coach, Member
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties / Thuộc tính điều hướng
        public Member? Member { get; set; }
        public Coach? Coach { get; set; }
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}
