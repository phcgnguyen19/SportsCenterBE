using SportsCenterAPI.Enums;

namespace SportsCenterAPI.Models.DTOs.Class
{
    public class SportClassMemberResponse
    {
        public int RegistrationId { get; set; }
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;

        public BookingStatus Status { get; set; }
        public DateTime BookedAt { get; set; }
    }
}
