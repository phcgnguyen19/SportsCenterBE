namespace SportsCenterAPI.Models
{
    /// <summary>
    /// Personalized training plan designed for a member by a coach.
    /// Thực thể kế hoạch tập luyện cá nhân do huấn luyện viên lập cho hội viên.
    /// </summary>
    public class TrainingPlan
    {
        public int Id { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string? Goal { get; set; } // WeightLoss, MuscleGain, Endurance
        public int MemberId { get; set; }
        public Member Member { get; set; } = null!;
        public int? CoachId { get; set; }
        public Coach? Coach { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Active";

        // Navigation properties
        public ICollection<TrainingPlanExercise> Exercises { get; set; } = new List<TrainingPlanExercise>();
    }
}
