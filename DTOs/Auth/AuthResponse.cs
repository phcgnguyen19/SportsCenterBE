namespace SportsCenterAPI.DTOs.Auth;

/// <summary>
/// Response DTO after successful authentication / DTO trả về sau khi xác thực thành công
/// </summary>
public class AuthResponse
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
