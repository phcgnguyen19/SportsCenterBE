namespace SportsCenterAPI.Models
{
    /// <summary>
    /// System notification sent to a user.
    /// Thực thể thông báo hệ thống gửi đến người dùng.
    /// </summary>
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
