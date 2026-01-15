using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduConnect.Models
{
    public class SemesterResult
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string StudentId { get; set; }

        [Required]
        public required string Semester { get; set; } // e.g., "Fall 2025", "Spring 2026"

        [Required]
        public required string CourseName { get; set; }

        [Required]
        public decimal MarksObtained { get; set; } // Out of 100

        [Required]
        public required string Grade { get; set; } // A, B, C, D, F

        [Required]
        public decimal GPA { get; set; } // 4.0 scale

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("StudentId")]
        public ApplicationUser? Student { get; set; }
    }
}
