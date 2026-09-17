namespace SportsCenterAPI.Models.DTOs.Class
{
    public class SportClassResponse
    {
        public int Id { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public int SportId { get; set; }

        public string SportName { get; set; } = string.Empty;

        public int? CoachId { get; set; }

        public decimal Price { get; set; }

        public int MaxCapacity { get; set; }

        public int RegisteredCount { get; set; }

        public int AvailableSlots => MaxCapacity - RegisteredCount;

        public string? Schedule { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }
    }
}
