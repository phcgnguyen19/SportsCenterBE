namespace SportsCenterAPI.DTOs.Response
{
    public class LoginResponseDTO
    {
        public string? Token { get; set; }

        public UserDTO? userDTO { get; set; }
    }
}
