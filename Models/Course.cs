namespace EduConnect.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? FacultyId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        public ApplicationUser? Faculty { get; set; }
        public ICollection<Topic>? Topics { get; set; }
        public ICollection<Material>? Materials { get; set; }
        public ICollection<Enrollment>? Enrollments { get; set; }
        public ICollection<Quiz>? Quizzes { get; set; }
    }
}
