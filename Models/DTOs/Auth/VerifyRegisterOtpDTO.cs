namespace SportsCenterAPI.Models.DTOs.Auth;

public class VerifyRegisterOtpDTO
{
    public string Email { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}