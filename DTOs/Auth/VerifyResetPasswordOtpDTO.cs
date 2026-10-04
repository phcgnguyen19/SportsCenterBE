namespace SportsCenterAPI.DTOs.Auth;

public class VerifyResetPasswordOtpDTO
{
    public string Email { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}