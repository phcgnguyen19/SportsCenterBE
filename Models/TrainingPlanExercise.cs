namespace SportsCenterAPI.Models
{
    /// <summary>
    /// Association entity mapping exercises into a training plan with sets, reps, and durations.
    /// Thực thể liên kết bài tập vào kế hoạch tập luyện cùng số hiệp (sets), số lần (reps) và thời lượng.
    /// </summary>
    public class TrainingPlanExercise
    {
        public int Id { get; set; }
        public int TrainingPlanId { get; set; }
        public TrainingPlan TrainingPlan { get; set; } = null!;
        public int ExerciseId { get; set; }
        public Exercise Exercise { get; set; } = null!;
        public int Sets { get; set; }
        public int Reps { get; set; }
        public int? DurationInMinutes { get; set; }
        public string? Notes { get; set; }
    }
}
