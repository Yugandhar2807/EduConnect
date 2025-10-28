using System.ComponentModel.DataAnnotations;

namespace EduConnect.Models
{
    public class RoadmapTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        // Category: Programming Language, Career Path, etc.
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        // Difficulty: Beginner, Intermediate, Advanced
        [MaxLength(50)]
        public string Level { get; set; } = "Beginner";

        // Icon for display
        [MaxLength(50)]
        public string? Icon { get; set; }

        // Color theme for the roadmap
        [MaxLength(20)]
        public string? Color { get; set; }

        // Estimated duration in hours
        public int EstimatedHours { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<RoadmapTopic>? Topics { get; set; }
        public ICollection<StudentRoadmapProgress>? StudentProgress { get; set; }
    }
}
