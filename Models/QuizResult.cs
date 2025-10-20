namespace EduConnect.Models
{
    public class QuizResult
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public string? StudentId { get; set; }
        public int MarksObtained { get; set; }
        public int TotalMarks { get; set; }
        public double PercentageScore { get; set; }
        public bool IsPassed { get; set; }
        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
        public int DurationTakenInSeconds { get; set; }

        public Quiz? Quiz { get; set; }
        public ApplicationUser? Student { get; set; }
    }
}
