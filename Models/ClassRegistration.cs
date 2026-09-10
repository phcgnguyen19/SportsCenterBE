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
    }
}
