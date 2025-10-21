using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduConnect.Models
{
    public class TopicProgress
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual ApplicationUser Student { get; set; }

        public int? TopicId { get; set; }

        [ForeignKey(nameof(TopicId))]
        public virtual Topic Topic { get; set; }

        public int? MaterialId { get; set; }

        [ForeignKey(nameof(MaterialId))]
        public virtual Material Material { get; set; }

        [Required]
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }
}
