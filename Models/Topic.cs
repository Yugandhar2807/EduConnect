using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduConnect.Models
{
    public class Topic
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [ForeignKey(nameof(CourseId))]
        public virtual Course Course { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        // PDF file path for the topic
        [StringLength(500)]
        public string PdfFilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for materials related to this topic
        public virtual ICollection<Material> Materials { get; set; } = new List<Material>();

        // Navigation property for quizzes related to this topic
        public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    }
}
