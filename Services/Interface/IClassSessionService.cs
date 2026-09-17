using SportsCenterAPI.Models.DTOs.Class;

namespace SportsCenterAPI.Services.Interface
{
    public interface IClassSessionService
    {
        Task<IEnumerable<SportClassSessionResponse>> GetSessionsAsync(
        int? classId,
        DateTime? fromDate,
        DateTime? toDate);

        Task<IEnumerable<SportClassSessionResponse>> GetCoachScheduleAsync(
            int coachId);

        Task<IEnumerable<SportClassMemberResponse>> GetSessionMembersAsync(
            int sessionId,
            int coachId);
    }
}
