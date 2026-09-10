namespace SportsCenterAPI.Models
{
    /// <summary>
    /// System audit log recording user actions and system events.
    /// Thực thể nhật ký kiểm toán ghi lại các thao tác người dùng và sự kiện hệ thống.
    /// </summary>
    public class AuditLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
        public string Action { get; set; } = string.Empty; // Create, Update, Delete, Login, Logout
        public string EntityName { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
