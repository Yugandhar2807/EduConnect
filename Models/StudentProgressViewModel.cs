using System.ComponentModel.DataAnnotations;

namespace EduConnect.Models
{
    /// <summary>
    /// ViewModel for comprehensive student progress dashboard
    /// Aggregates data from multiple sources for display
    /// </summary>
    public class StudentProgressViewModel
    {
        // Student Information
        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Display(Name = "Student Name")]
        public string StudentName { get; set; } = string.Empty;

        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        // Aggregated Metrics
        [Display(Name = "Attendance Percentage")]
        public double AttendancePercentage { get; set; } = 0;

        [Display(Name = "Total Present Days")]
        public int TotalPresent { get; set; } = 0;

        [Display(Name = "Total Absent Days")]
        public int TotalAbsent { get; set; } = 0;

        [Display(Name = "Total Leave Days")]
        public int TotalLeave { get; set; } = 0;

        [Display(Name = "Average Course Progress")]
        public double AverageCourseProgress { get; set; } = 0;

        [Display(Name = "Active Courses")]
        public int ActiveCourses { get; set; } = 0;

        [Display(Name = "Completed Courses")]
        public int CompletedCourses { get; set; } = 0;

        [Display(Name = "Average GPA")]
        public double AverageGPA { get; set; } = 0;

        [Display(Name = "Total Semesters")]
        public int TotalSemesters { get; set; } = 0;

        // Detailed Collections
        [Display(Name = "Course Progress Details")]
        public List<CourseProgressDetail> CourseProgressDetails { get; set; } = new();

        [Display(Name = "Semester Results")]
        public List<SemesterResultDetail> SemesterResultDetails { get; set; } = new();

        [Display(Name = "Attendance Breakdown")]
        public List<AttendanceBreakdownDetail> AttendanceBreakdown { get; set; } = new();

        // Chart Data (JSON for front-end)
        public string CourseProgressChartJson { get; set; } = "{}";
        public string GpaTrendChartJson { get; set; } = "{}";
        public string AttendanceChartJson { get; set; } = "{}";
    }

    /// <summary>
    /// Represents course progress information for a student
    /// </summary>
    public class CourseProgressDetail
    {
        [Display(Name = "Course Name")]
        public string CourseName { get; set; } = string.Empty;

        [Display(Name = "Completion Percentage")]
        public double CompletionPercentage { get; set; } = 0;

        [Display(Name = "Topics Completed")]
        public int TopicsCompleted { get; set; } = 0;

        [Display(Name = "Total Topics")]
        public int TotalTopics { get; set; } = 0;

        [Display(Name = "Quizzes Taken")]
        public int QuizzesTaken { get; set; } = 0;

        [Display(Name = "Average Quiz Score")]
        public double AverageScore { get; set; } = 0;

        [Display(Name = "Progress Status")]
        public string ProgressStatus { get; set; } = "Not Started";
    }

    /// <summary>
    /// Represents semester result information for a student
    /// </summary>
    public class SemesterResultDetail
    {
        [Display(Name = "Semester")]
        public string Semester { get; set; } = string.Empty;

        [Display(Name = "Course Name")]
        public string CourseName { get; set; } = string.Empty;

        [Display(Name = "Marks Obtained")]
        public double MarksObtained { get; set; } = 0;

        [Display(Name = "Grade")]
        public string Grade { get; set; } = string.Empty;

        [Display(Name = "GPA")]
        public double GPA { get; set; } = 0;
    }

    /// <summary>
    /// Represents monthly attendance breakdown for a student
    /// </summary>
    public class AttendanceBreakdownDetail
    {
        [Display(Name = "Month")]
        public string Month { get; set; } = string.Empty;

        [Display(Name = "Present")]
        public int Present { get; set; } = 0;

        [Display(Name = "Absent")]
        public int Absent { get; set; } = 0;

        [Display(Name = "Leave")]
        public int Leave { get; set; } = 0;
    }
}
