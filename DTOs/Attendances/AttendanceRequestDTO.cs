using System.ComponentModel.DataAnnotations;

namespace SportsCenterAPI.DTOs.Attendances
{
    public class MarkAttendanceDTO
    {
        [Required]
        public int MemberId { get; set; }

        [Required]
        public int SessionId { get; set; }
        [Required]
        [RegularExpression("^(Present|Absent|Excused)$", ErrorMessage = "Status must be Present, Absent, or Excused")]
        public string Status { get; set; } = "Present";
    }

    public class AttendanceResponseDTO
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public DateTime CheckInTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? SessionId { get; set; }
    }
}
