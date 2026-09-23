namespace SportsCenterAPI.Models
{
    /// <summary>
    /// Registration of a member into a specific sport class.
    /// Thực thể đăng ký tham gia lớp học thể thao của hội viên.
    /// </summary>
    public class ClassRegistration
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;
        public int ClassId { get; set; }
        public SportClass Class { get; set; } = null!;
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Registered"; // Registered, Cancelled, Completed
        // Null only for historical class-level registrations, before session booking was introduced.
        public int? SessionId { get; set; }
        public ClassSession? Session { get; set; }
        public int? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
        public int? CancelledByUserId { get; set; }
        public User? CancelledByUser { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? CancellationDeadline { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(500)]
        public string? CancellationReason { get; set; }
        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[] RowVersion { get; set; } = [];
    }
}
