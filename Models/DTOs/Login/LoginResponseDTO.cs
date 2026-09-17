using SportsCenterAPI.Models.DTOs.User;

namespace SportsCenterAPI.Models.DTOs.Login
{
    public class LoginResponseDTO
    {
        public string? Token { get; set; }

        public UserDTO? userDTO { get; set; }
    }
}
