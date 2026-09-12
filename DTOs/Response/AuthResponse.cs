using SportsCenterAPI.Models;
using System.Data;

namespace SportsCenterAPI.DTOs.Response
{
    public class AuthResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}


////UserId = user.Id,
//Email = user.Email,
//            FullName = user.FullName,
//            Role = user.Role,
//            Token = token