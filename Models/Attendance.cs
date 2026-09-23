namespace SportsCenterAPI.Models
{
    /// <summary>
    /// Attendance record tracking member check-ins for classes.
    /// Thực thể ghi nhận điểm danh lớp học của hội viên.
    /// </summary>
    public class Attendance
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;
        public int ClassId { get; set; }
        public SportClass Class { get; set; } = null!;
        public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Present"; // Present, Absent, Excused
        public int? SessionId { get; set; }
        public ClassSession? Session { get; set; }
    }
}
