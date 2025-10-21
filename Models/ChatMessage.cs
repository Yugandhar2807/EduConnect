using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduConnect.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required]
        [Column(TypeName = "TEXT")]
        public string Message { get; set; }

        [Required]
        public MessageSender Sender { get; set; } // User or Bot

        [Column(TypeName = "TEXT")]
        public string? Response { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ChatCategory Category { get; set; }

        public double? Sentiment { get; set; } // 0-1: 0=negative, 1=positive

        public bool IsResolved { get; set; } = false;
    }

    public enum MessageSender
    {
        User,
        Bot
    }

    public enum ChatCategory
    {
        General,
        CourseInquiry,
        TechnicalSupport,
        EnrollmentHelp,
        GradeInquiry,
        MaterialAccess,
        QuizSupport,
        Feedback,
        Other
    }
}
