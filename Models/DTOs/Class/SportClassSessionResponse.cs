using SportsCenterAPI.Enums;

namespace SportsCenterAPI.Models.DTOs.Class
{
    public class SportClassSessionResponse
    {
        public int Id { get; set; }

        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;

        public int CoachId { get; set; }
        public string CoachName { get; set; } = string.Empty;

        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int Capacity { get; set; }
        public int BookedCount { get; set; }
        public int AvailableSlots => Capacity - BookedCount;

        public ClassSessionStatus Status { get; set; }
    }
}
