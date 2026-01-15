using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduConnect.Models
{
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string StudentId { get; set; }

        public int? CourseId { get; set; } // Optional - for general attendance

        [Required]
        public DateTime AttendanceDate { get; set; }

        [Required]
        public required string Status { get; set; } // Present, Absent, Leave

        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("StudentId")]
        public ApplicationUser? Student { get; set; }

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }
    }
}
