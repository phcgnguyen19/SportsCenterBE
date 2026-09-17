using System.ComponentModel.DataAnnotations;

namespace SportsCenterAPI.Models.DTOs.Login;

/// <summary>
/// Request DTO for user login / DTO yêu cầu đăng nhập
/// </summary>
public class LoginRequestDTO
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}
