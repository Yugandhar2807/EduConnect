namespace EduConnect.Models
{
    public class Announcement
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? FacultyId { get; set; }
        public int? CourseId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public ApplicationUser? Faculty { get; set; }
        public Course? Course { get; set; }
    }
}
