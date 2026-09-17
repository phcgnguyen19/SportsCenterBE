using System.ComponentModel.DataAnnotations;

namespace SportsCenterAPI.Models.DTOs.Class
{
    public class SportClassCreateRequestDTO
    {
        [Required(ErrorMessage = "Enter Class Name")]
        public string ClassName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Please select a sport.")]
        public int SportId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Invalid coach ID.")]
        public int? CoachId { get; set; }

        [Range(typeof(decimal), "0", "9999999999999999.99",
            ErrorMessage = "Price must be a positive number within the allowed range.")]


        public decimal Price { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Max capacity must be a positive number.")]
        public int MaxCapacity { get; set; }

        public string? Schedule { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }
}
