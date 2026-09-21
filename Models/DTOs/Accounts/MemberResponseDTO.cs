namespace SportsCenterAPI.Models.DTOs.Accounts;

public class MemberResponseDTO
{
    public int MemberId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? FitnessGoal { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    internal static MemberResponseDTO FromModel(Member member) => new()
    {
        MemberId = member.Id,
        UserId = member.UserId,
        FullName = member.User.FullName,
        Email = member.User.Email,
        Phone = member.User.PhoneNumber,
        IsActive = member.User.IsActive,
        DateOfBirth = member.DateOfBirth,
        Gender = member.Gender,
        Address = member.Address,
        FitnessGoal = member.FitnessGoal,
        CreatedAt = member.User.CreatedAt,
        UpdatedAt = member.User.UpdatedAt
    };
}
