using System.ComponentModel.DataAnnotations;

namespace SportsCenterAPI.Models.DTOs.Class
{
    public class SportClassUpdateRequestDTO
    {
        [Required]
        public string ClassName { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int SportId { get; set; }

        [Range(1, int.MaxValue)]
        public int? CoachId { get; set; }

        [Range(typeof(decimal), "0", "9999999999999999.99")]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue)]
        public int MaxCapacity { get; set; }

        public string? Schedule { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }
}
