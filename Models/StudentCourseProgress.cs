using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduConnect.Models
{
    public class StudentCourseProgress
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string StudentId { get; set; }

        [Required]
        public int CourseId { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

        [Required]
        public int TopicsCompleted { get; set; } // Number of topics completed

        public int? TotalTopics { get; set; } // Total topics in course

        [Required]
        public decimal CompletionPercentage { get; set; } // 0-100

        [Required]
        public int QuizzesTaken { get; set; } // Number of quizzes attempted

        [Required]
        public decimal AverageScore { get; set; } // Average quiz score

        [Required]
        public required string ProgressStatus { get; set; } // Not Started, In Progress, Completed

        public DateTime LastActivityDate { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        [ForeignKey("StudentId")]
        public ApplicationUser? Student { get; set; }

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }
    }
}
