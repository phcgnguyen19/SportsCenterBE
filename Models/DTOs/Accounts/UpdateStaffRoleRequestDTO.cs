using System.ComponentModel.DataAnnotations;

namespace SportsCenterAPI.Models.DTOs.Accounts;

public class UpdateStaffRoleRequestDTO
{
    [Required, RegularExpression("^(Manager|Receptionist|Coach)$",
        ErrorMessage = "Role must be Manager, Receptionist or Coach.")]
    public string Role { get; set; } = string.Empty;
}
