using SportsCenterAPI.Enums;

namespace SportsCenterAPI.Models.DTOs.Class
{
    public class SportClassBookingResponse
    {
        public int Id { get; set; }
        public int MemberId { get; set; }

        public SportClassSessionResponse Session { get; set; }
            = new SportClassSessionResponse();

        public BookingStatus Status { get; set; }

        public DateTime BookedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
    }

}
