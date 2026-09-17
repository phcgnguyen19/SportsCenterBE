namespace SportsCenterAPI.Models.DTOs.Class
{
    public class SportClassSearchRequest
    {
        public string? Keyword { get; set; }

        public int? SportId { get; set; }

        public int? CoachId { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }
    }
}
