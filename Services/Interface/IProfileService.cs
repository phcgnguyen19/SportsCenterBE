using SportsCenterAPI.DTOs.Profile;

namespace SportsCenterAPI.Services.Interface;

public interface IProfileService
{
    Task<ProfileDTO> GetAsync(int userId);

    Task<ProfileDTO> UpdateAsync(int userId, UpdateProfileDTO dto);
}