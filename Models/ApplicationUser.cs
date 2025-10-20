using Microsoft.AspNetCore.Identity;

namespace EduConnect.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public ICollection<Enrollment>? Enrollments { get; set; }
        public ICollection<Course>? CreatedCourses { get; set; }
        public ICollection<QuizResult>? QuizResults { get; set; }
    }
}
