using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduConnect.Models
{
    public class RoadmapNode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        // For tree structure
        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public RoadmapNode? Parent { get; set; }

        public ICollection<RoadmapNode>? Children { get; set; }

        // Order for displaying nodes at the same level
        public int Order { get; set; }

        // Track completion per student
        public string? StudentId { get; set; }

        [ForeignKey("StudentId")]
        public ApplicationUser? Student { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional: Link to course or topic
        public int? CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        public int? TopicId { get; set; }

        [ForeignKey("TopicId")]
        public Topic? Topic { get; set; }

        // Icon or color for visual representation
        [MaxLength(50)]
        public string? Icon { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; }
    }
}
