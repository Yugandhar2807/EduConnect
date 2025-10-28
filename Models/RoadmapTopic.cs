using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduConnect.Models
{
    public class RoadmapTopic
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RoadmapTemplateId { get; set; }

        [ForeignKey("RoadmapTemplateId")]
        public RoadmapTemplate? RoadmapTemplate { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        // Position in the tree (for parent-child relationship)
        public int? ParentTopicId { get; set; }

        [ForeignKey("ParentTopicId")]
        public RoadmapTopic? ParentTopic { get; set; }

        public ICollection<RoadmapTopic>? ChildTopics { get; set; }

        // Display order at the same level
        public int OrderIndex { get; set; }

        // Level in the tree (0 = root, 1 = first level, etc.)
        public int Level { get; set; }

        // Position for visual layout (X, Y coordinates in percentage)
        public int PositionX { get; set; }
        public int PositionY { get; set; }

        // Resources
        [MaxLength(4000)]
        public string? FreeResources { get; set; } // JSON array of links

        [MaxLength(4000)]
        public string? PaidResources { get; set; } // JSON array of links

        [MaxLength(2000)]
        public string? AITutorPrompt { get; set; } // Prompt for AI tutor

        // Visual styling
        [MaxLength(50)]
        public string? Icon { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
