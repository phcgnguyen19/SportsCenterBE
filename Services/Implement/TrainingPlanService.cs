using Microsoft.EntityFrameworkCore;
using SportsCenterAPI.Data;
using SportsCenterAPI.Models;
using SportsCenterAPI.DTOs.Training;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Services.Implement;

public class TrainingPlanService(AppDbContext context) : ITrainingPlanService
{
    public async Task<TrainingPlanResponseDTO> CreatePlanAsync(int coachUserId, CreateTrainingPlanDTO request)
    {
        var coach = await context.Coaches.FirstOrDefaultAsync(c => c.UserId == coachUserId)
            ?? throw new UnauthorizedAccessException("Only coaches can create training plans.");

        var plan = new TrainingPlan
        {
            PlanName = request.PlanName,
            Goal = request.Goal,
            MemberId = request.MemberId,
            CoachId = coach.Id,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = "Active",
            Exercises = request.Exercises.Select(e => new TrainingPlanExercise
            {
                ExerciseId = e.ExerciseId,
                Sets = e.Sets,
                Reps = e.Reps,
                DurationInMinutes = e.DurationInMinutes,
                Notes = e.Notes
            }).ToList()
        };

        context.TrainingPlans.Add(plan);
        await context.SaveChangesAsync();

        return await GetPlanByIdAsync(plan.Id);
    }

    public async Task<IEnumerable<TrainingPlanResponseDTO>> GetMemberPlansAsync(int memberId)
    {
        var plans = await context.TrainingPlans
            .Where(p => p.MemberId == memberId)
            .Select(p => p.Id)
            .ToListAsync();

        var result = new List<TrainingPlanResponseDTO>();
        foreach (var id in plans)
        {
            result.Add(await GetPlanByIdAsync(id));
        }
        return result;
    }

    public async Task<bool> UpdateExerciseResultAsync(int coachUserId, int planExerciseId, UpdateExerciseResultDTO request)
    {
        var coach = await context.Coaches.FirstOrDefaultAsync(c => c.UserId == coachUserId)
            ?? throw new UnauthorizedAccessException("User is not a coach.");

        var exercise = await context.TrainingPlanExercises
            .Include(e => e.TrainingPlan)
            .FirstOrDefaultAsync(e => e.Id == planExerciseId)
            ?? throw new ArgumentException("Exercise not found in any plan.");

        if (exercise.TrainingPlan.CoachId != coach.Id)
            throw new UnauthorizedAccessException("You can only update your own training plans.");

        exercise.Notes = request.Notes; // Ghi nhận đánh giá/tiến độ
        await context.SaveChangesAsync();
        return true;
    }

    private async Task<TrainingPlanResponseDTO> GetPlanByIdAsync(int id)
    {
        var p = await context.TrainingPlans
            .Include(x => x.Member).ThenInclude(m => m.User)
            .Include(x => x.Coach).ThenInclude(c => c.User)
            .Include(x => x.Exercises).ThenInclude(e => e.Exercise)
            .FirstAsync(x => x.Id == id);

        return new TrainingPlanResponseDTO
        {
            Id = p.Id,
            PlanName = p.PlanName,
            Goal = p.Goal ?? "",
            MemberName = p.Member.User.FullName,
            CoachName = p.Coach?.User.FullName ?? "N/A",
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Status = p.Status,
            Exercises = p.Exercises.Select(e => new PlanExerciseResponseDTO
            {
                Id = e.Id,
                ExerciseName = e.Exercise?.ExerciseName ?? "Unknown",
                Sets = e.Sets,
                Reps = e.Reps,
                DurationInMinutes = e.DurationInMinutes,
                Notes = e.Notes
            }).ToList()
        };
    }
}