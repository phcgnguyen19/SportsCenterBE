using System.ComponentModel.DataAnnotations;

namespace SportsCenterAPI.DTOs.Training;

public class CreateTrainingPlanDTO
{
    [Required] public string PlanName { get; set; } = string.Empty;
    [Required] public string Goal { get; set; } = string.Empty; // WeightLoss, MuscleGain...
    [Required] public int MemberId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<CreatePlanExerciseDTO> Exercises { get; set; } = new();
}

public class CreatePlanExerciseDTO
{
    [Required] public int ExerciseId { get; set; }
    [Required] public int Sets { get; set; }
    [Required] public int Reps { get; set; }
    public int? DurationInMinutes { get; set; }
    public string? Notes { get; set; }
}

public class TrainingPlanResponseDTO
{
    public int Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string CoachName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<PlanExerciseResponseDTO> Exercises { get; set; } = new();
}

public class PlanExerciseResponseDTO
{
    public int Id { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public int? DurationInMinutes { get; set; }
    public string? Notes { get; set; }
}

public class UpdateExerciseResultDTO
{
    [Required] public string Notes { get; set; } = string.Empty;
}