using SportsCenterAPI.Models.DTOs.Class;

namespace SportsCenterAPI.Services.Interface
{
    public interface ISportClassService
    {
        Task<IEnumerable<SportClassResponse>> GetClassesAsync(
            SportClassSearchRequest request);

        Task<SportClassResponse?> GetClassByIdAsync(int classId);

        Task<IEnumerable<SportClassResponse>> GetCoachClassesAsync(
            int coachId);

        Task<IEnumerable<SportClassMemberResponse>> GetClassMembersAsync(
            int classId,
            int coachId);

        Task<SportClassResponse> CreateClassAsync(SportClassCreateRequestDTO sportClassCreateRequestdto);

        Task<SportClassResponse?> UpdateClassAsync(
            int classId,
            SportClassUpdateRequestDTO sportClassUpdateRequestdto);

        Task<bool> DeleteClassAsync(int classId);
    }
}
