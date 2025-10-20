namespace EduConnect.Models
{
    public class Quiz
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int CourseId { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalMarks { get; set; }
        public int PassingMarks { get; set; }
        public int DurationInMinutes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public Course? Course { get; set; }
        public ICollection<QuizQuestion>? Questions { get; set; }
        public ICollection<QuizResult>? Results { get; set; }
    }
}
