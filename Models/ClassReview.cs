using System.ComponentModel.DataAnnotations;

namespace SportsCenterAPI.Models;

public class ClassReview
{
    public int Id { get; set; }
    public int RegistrationId { get; set; }
    public ClassRegistration Registration { get; set; } = null!;
    public int Rating { get; set; }
    [MaxLength(2000)] public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
