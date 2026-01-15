using System.ComponentModel.DataAnnotations;

namespace EduConnect.Models
{
    /// <summary>
    /// ViewModel for student analytics and Power BI dashboard
    /// </summary>
    public class StudentAnalyticsViewModel
    {
        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Display(Name = "Student Name")]
        public string? StudentName { get; set; }

        [Display(Name = "Email Address")]
        public string? Email { get; set; }

        [Display(Name = "Dashboard Title")]
        public string DashboardTitle => $"{StudentName}'s Academic Analytics Dashboard";

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated => DateTime.UtcNow;
    }
}
