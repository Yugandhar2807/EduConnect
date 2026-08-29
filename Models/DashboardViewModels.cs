namespace EduConnect.Models
{
    /// <summary>Simple label/value pair used to feed charts from Razor views.</summary>
    public record ChartPoint(string Label, double Value);

    // ==================== ADMIN ====================

    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalFaculty { get; set; }
        public int TotalCourses { get; set; }
        public int ActiveCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public int TotalQuizAttempts { get; set; }
        public double AverageStudentProgress { get; set; }
        public double QuizPassRate { get; set; }

        public List<ChartPoint> EnrollmentsByMonth { get; set; } = new();
        public List<ChartPoint> TopCoursesByEnrollment { get; set; } = new();
        public List<ChartPoint> UsersByRole { get; set; } = new();

        public List<RecentActivityItem> RecentEnrollments { get; set; } = new();
        public List<RecentActivityItem> RecentQuizAttempts { get; set; } = new();
    }

    public class RecentActivityItem
    {
        public string? StudentName { get; set; }
        public string? Target { get; set; }
        public string? Detail { get; set; }
        public DateTime OccurredAt { get; set; }
        public bool? Success { get; set; }
    }

    public class AdminAnalyticsViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalFaculty { get; set; }
        public int TotalCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public int ActiveStudents { get; set; }
        public double OverallAttendanceRate { get; set; }
        public double AverageGpa { get; set; }

        public List<CourseStatItem> CourseStats { get; set; } = new();
        public List<FacultyStatItem> FacultyStats { get; set; } = new();
        public List<ChartPoint> GradeDistribution { get; set; } = new();
        public List<ChartPoint> AttendanceByMonth { get; set; } = new();
    }

    public class CourseStatItem
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Category { get; set; }
        public string? FacultyName { get; set; }
        public int EnrollmentCount { get; set; }
        public double AverageProgress { get; set; }
        public int QuizCount { get; set; }
        public double AverageQuizScore { get; set; }
        public bool IsActive { get; set; }
    }

    public class FacultyStatItem
    {
        public string? FacultyName { get; set; }
        public string? Department { get; set; }
        public int CourseCount { get; set; }
        public int StudentCount { get; set; }
    }

    // ==================== FACULTY ====================

    public class FacultyDashboardViewModel
    {
        public int TotalCourses { get; set; }
        public int TotalStudents { get; set; }
        public int TotalQuizzes { get; set; }
        public int TotalAnnouncements { get; set; }
        public List<FacultyCourseItem> Courses { get; set; } = new();
        public List<RecentActivityItem> RecentQuizAttempts { get; set; } = new();
        public List<ChartPoint> EnrollmentsPerCourse { get; set; } = new();
    }

    public class FacultyCourseItem
    {
        public Course Course { get; set; } = null!;
        public int EnrollmentCount { get; set; }
        public int TopicCount { get; set; }
        public int MaterialCount { get; set; }
        public int QuizCount { get; set; }
        public double AverageProgress { get; set; }
    }

    public class CourseStudentsViewModel
    {
        public Course Course { get; set; } = null!;
        public List<CourseStudentItem> Students { get; set; } = new();
    }

    public class CourseStudentItem
    {
        public string StudentId { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Email { get; set; }
        public DateTime EnrolledAt { get; set; }
        public int ProgressPercentage { get; set; }
        public bool IsCompleted { get; set; }
        public int QuizzesTaken { get; set; }
        public double AverageQuizScore { get; set; }
        public DateTime? LastQuizAttempt { get; set; }
    }

    public class QuizDetailsViewModel
    {
        public Quiz Quiz { get; set; } = null!;
        public int AttemptCount { get; set; }
        public double AverageScore { get; set; }
        public double PassRate { get; set; }
        public List<QuizResult> RecentResults { get; set; } = new();
    }

    // ==================== STUDENT ====================

    public class StudentDashboardViewModel
    {
        public string? StudentName { get; set; }
        public int EnrolledCourses { get; set; }
        public int CompletedCourses { get; set; }
        public double AttendancePercentage { get; set; }
        public double? AverageGpa { get; set; }
        public double AverageQuizScore { get; set; }
        public List<Enrollment> Enrollments { get; set; } = new();
        public List<Announcement> Announcements { get; set; } = new();
        public List<QuizResult> RecentQuizResults { get; set; } = new();
        public List<Attendance> RecentAttendance { get; set; } = new();
    }

    public class StudentCourseViewModel
    {
        public Course Course { get; set; } = null!;
        public Enrollment Enrollment { get; set; } = null!;
        public HashSet<int> CompletedTopicIds { get; set; } = new();
        public HashSet<int> CompletedMaterialIds { get; set; } = new();
        public Dictionary<int, QuizResult> BestQuizResults { get; set; } = new();
        public Dictionary<int, int> QuizAttemptCounts { get; set; } = new();
        public double ProgressPercentage { get; set; }
    }

    public class StudentAttendanceViewModel
    {
        public int TotalPresent { get; set; }
        public int TotalAbsent { get; set; }
        public int TotalLeave { get; set; }
        public double AttendancePercentage { get; set; }
        public List<Attendance> Records { get; set; } = new();
        public List<AttendanceBreakdownDetail> MonthlyBreakdown { get; set; } = new();
    }

    public class StudentAnnouncementsViewModel
    {
        public List<Announcement> Announcements { get; set; } = new();
    }
}
