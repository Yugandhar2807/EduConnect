using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduConnect.Models
{
    public class StudentRoadmapProgress
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [ForeignKey("StudentId")]
        public ApplicationUser? Student { get; set; }

        [Required]
        public int RoadmapTemplateId { get; set; }

        [ForeignKey("RoadmapTemplateId")]
        public RoadmapTemplate? RoadmapTemplate { get; set; }

        // Comma-separated list of completed topic IDs
        [MaxLength(4000)]
        public string CompletedTopicIds { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public int ProgressPercentage { get; set; } = 0;

        public bool IsCompleted { get; set; } = false;
    }
}
