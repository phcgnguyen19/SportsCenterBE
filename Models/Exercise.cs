namespace SportsCenterAPI.Models
{
    /// <summary>
    /// Exercise catalog entity (e.g., Squat, Bench Press, Deadlift).
    /// Thực thể danh mục bài tập.
    /// </summary>
    public class Exercise
    {
        public int Id { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? MuscleGroup { get; set; } // Legs, Chest, Back, Arms, Core
        public string? VideoUrl { get; set; }

        // Navigation properties
        public ICollection<TrainingPlanExercise> TrainingPlanExercises { get; set; } = new List<TrainingPlanExercise>();
    }
}
