namespace SportsCenterAPI.DTOs
{
    public class UserDTO
    {
        public int Id { get; set; }
        public string FullName { get; set; } = default;

        public string Email { get; set; } = default;

        public string Role { get; set; } = default;
    }
}
