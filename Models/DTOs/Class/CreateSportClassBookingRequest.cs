using System.ComponentModel.DataAnnotations;

namespace SportsCenterAPI.Models.DTOs.Class
{
    public class CreateSportClassBookingRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Invalid sport class ID")]
        public int SportClassId { get; set; }
    }
}
