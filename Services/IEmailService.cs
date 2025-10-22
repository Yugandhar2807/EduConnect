namespace EduConnect.Services
{
    /// <summary>
    /// Service interface for sending emails
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send a simple email
        /// </summary>
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent);

        /// <summary>
        /// Send email to multiple recipients
        /// </summary>
        Task<bool> SendBulkEmailAsync(List<string> toEmails, string subject, string htmlContent);

        /// <summary>
        /// Send enrollment confirmation email
        /// </summary>
        Task SendEnrollmentConfirmationAsync(string studentEmail, string studentName, string courseName);

        /// <summary>
        /// Send grade notification email
        /// </summary>
        Task SendGradeNotificationAsync(string studentEmail, string studentName, string courseName, string quizName, int score, int totalPoints);

        /// <summary>
        /// Send announcement notification to multiple students
        /// </summary>
        Task SendAnnouncementAsync(List<string> studentEmails, string courseName, string announcementTitle, string announcementContent, string facultyName);

        /// <summary>
        /// Send welcome email to new user
        /// </summary>
        Task SendWelcomeEmailAsync(string email, string fullName, string role);

        /// <summary>
        /// Send password reset email
        /// </summary>
        Task SendPasswordResetEmailAsync(string email, string resetLink);

        /// <summary>
        /// Send course completion certificate
        /// </summary>
        Task SendCertificateEmailAsync(string studentEmail, string studentName, string courseName, DateTime completionDate);
    }
}
