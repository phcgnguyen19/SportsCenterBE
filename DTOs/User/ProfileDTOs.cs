using System.ComponentModel.DataAnnotations;

namespace SportsCenterAPI.DTOs.Profile;

public record ProfileDTO(
    int UserId,
    string Email,
    string FullName,
    string? PhoneNumber,
    string RoleName,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public class UpdateProfileDTO
{
    [StringLength(200, MinimumLength = 1)]
    public string? FullName { get; set; }

    [Phone]
    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    [StringLength(254)]
    public string? Email { get; set; }

    [StringLength(72, MinimumLength = 8)]
    public string? Password { get; set; }

    public string? CurrentPassword { get; set; }
}

public class AvatarUploadDTO
{
    [Required]
    public IFormFile File { get; set; } = null!;
}