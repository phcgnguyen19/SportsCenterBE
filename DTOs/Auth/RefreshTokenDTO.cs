using System.ComponentModel.DataAnnotations;

namespace SportsCenterAPI.DTOs.Auth
{
    public class RefreshTokenDTO
    {
        [Required]
        [StringLength(200)]
        public string RefreshTokenKey { get; set; } = string.Empty;
    }
}
