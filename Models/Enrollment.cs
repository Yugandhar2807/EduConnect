namespace EduConnect.Models
{
    public class Enrollment
    {
        public int Id { get; set; }
        public string? StudentId { get; set; }
        public int CourseId { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public int ProgressPercentage { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;

        public ApplicationUser? Student { get; set; }
        public Course? Course { get; set; }
    }
}
