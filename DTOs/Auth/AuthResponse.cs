using System.Data;

namespace SportsCenterAPI.DTOs.Auth
{
    public class AuthResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string RefreshTokenKey { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}


////UserId = user.Id,
//Email = user.Email,
//            FullName = user.FullName,
//            Role = user.Role,
//            Token = token
