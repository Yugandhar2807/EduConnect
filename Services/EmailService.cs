using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EduConnect.Services
{
    /// <summary>
    /// Email service implementation using Twilio SendGrid REST API
    /// Development mode uses mock/logging only
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly string _twilioAccountSid;
        private readonly string _twilioAuthToken;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly ILogger<EmailService> _logger;
        private readonly HttpClient _httpClient;
        private readonly bool _isDevelopment;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IWebHostEnvironment env)
        {
            _twilioAccountSid = configuration["Twilio:AccountSid"] ?? "";
            _twilioAuthToken = configuration["Twilio:AuthToken"] ?? "";
            _logger = logger;
            _isDevelopment = env.IsDevelopment();
            
            if (string.IsNullOrEmpty(_twilioAccountSid) || string.IsNullOrEmpty(_twilioAuthToken))
            {
                _logger.LogWarning("⚠️ Twilio credentials not configured. Email service will run in MOCK MODE (development)");
            }

            _fromEmail = configuration["Twilio:FromEmail"] ?? "noreply@educonnect.com";
            _fromName = configuration["Twilio:FromName"] ?? "EduConnect";
            
            _httpClient = new HttpClient();
            // Set up Basic Auth for Twilio SendGrid API
            if (!string.IsNullOrEmpty(_twilioAccountSid) && !string.IsNullOrEmpty(_twilioAuthToken))
            {
                var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_twilioAccountSid}:{_twilioAuthToken}"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }
        }

        /// <summary>
        /// Send a simple email via Twilio SendGrid REST API (or mock in development)
        /// </summary>
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            try
            {
                // DEVELOPMENT MODE: Mock email sending if credentials are invalid (just log it)
                bool hasValidCredentials = !string.IsNullOrEmpty(_twilioAccountSid) && 
                                          !string.IsNullOrEmpty(_twilioAuthToken) &&
                                          _twilioAccountSid.StartsWith("SG."); // SendGrid API keys start with SG.

                if (_isDevelopment && !hasValidCredentials)
                {
                    _logger.LogInformation($"📧 [MOCK EMAIL - Development Mode] To: {toEmail}");
                    _logger.LogInformation($"📧 [MOCK EMAIL - Development Mode] Subject: {subject}");
                    _logger.LogInformation($"📧 [MOCK EMAIL - Development Mode] Content (first 100 chars): {htmlContent[..Math.Min(100, htmlContent.Length)]}...");
                    return true;
                }

                // PRODUCTION MODE: Send via Twilio SendGrid API
                var mailContent = new
                {
                    personalizations = new[]
                    {
                        new { to = new[] { new { email = toEmail } } }
                    },
                    from = new { email = _fromEmail, name = _fromName },
                    subject = subject,
                    content = new[] { new { type = "text/html", value = htmlContent } }
                };

                var json = JsonSerializer.Serialize(mailContent);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send")
                {
                    Content = content
                };
                
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"✅ Email sent successfully to {toEmail}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Failed to send email to {toEmail}. Status: {response.StatusCode}. Error: {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error sending email to {toEmail}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send email to multiple recipients
        /// </summary>
        public async Task<bool> SendBulkEmailAsync(List<string> toEmails, string subject, string htmlContent)
        {
            try
            {
                var recipients = toEmails.Select(email => new { email = email }).ToArray();
                
                var mailContent = new
                {
                    personalizations = new[]
                    {
                        new { to = recipients }
                    },
                    from = new { email = _fromEmail, name = _fromName },
                    subject = subject,
                    content = new[] { new { type = "text/html", value = htmlContent } }
                };

                var json = JsonSerializer.Serialize(mailContent);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send")
                {
                    Content = content
                };
                
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Bulk email sent to {toEmails.Count} recipients");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to send bulk email. Status: {response.StatusCode}. Error: {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending bulk email: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send enrollment confirmation email
        /// </summary>
        public async Task SendEnrollmentConfirmationAsync(string studentEmail, string studentName, string courseName)
        {
            var subject = $"Enrollment Confirmed - {courseName}";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 5px 5px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border-radius: 0 0 5px 5px; }}
        .button {{ background: #667eea; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block; margin-top: 15px; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #777; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to {courseName}!</h1>
        </div>
        <div class='content'>
            <p>Dear {studentName},</p>
            <p>Congratulations! Your enrollment in <strong>{courseName}</strong> has been confirmed.</p>
            <p>You now have access to:</p>
            <ul>
                <li>Course materials and lectures</li>
                <li>Quizzes and assignments</li>
                <li>Discussion forums</li>
                <li>Progress tracking</li>
            </ul>
            <p>Get started by logging into your EduConnect account and navigating to your dashboard.</p>
            <a href='http://localhost:8000/Student/CourseDetails' class='button'>View Course</a>
            <div class='footer'>
                <p>Best regards,<br/>The EduConnect Team</p>
                <p>This is an automated email. Please do not reply to this message.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(studentEmail, subject, htmlContent);
        }

        /// <summary>
        /// Send grade notification email
        /// </summary>
        public async Task SendGradeNotificationAsync(string studentEmail, string studentName, string courseName, string quizName, int score, int totalPoints)
        {
            var percentage = (score * 100) / totalPoints;
            var gradeColor = percentage >= 80 ? "#28a745" : percentage >= 60 ? "#ffc107" : "#dc3545";
            var gradeLetter = percentage >= 90 ? "A" : percentage >= 80 ? "B" : percentage >= 70 ? "C" : percentage >= 60 ? "D" : "F";

            var subject = $"Grade Posted - {quizName} ({gradeLetter})";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 5px 5px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border-radius: 0 0 5px 5px; }}
        .grade-box {{ background: white; padding: 20px; border-radius: 5px; border-left: 4px solid {gradeColor}; margin: 20px 0; }}
        .grade-number {{ font-size: 36px; font-weight: bold; color: {gradeColor}; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #777; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Your Grade is Ready!</h1>
        </div>
        <div class='content'>
            <p>Dear {studentName},</p>
            <p>Your grade for <strong>{quizName}</strong> in <strong>{courseName}</strong> has been posted.</p>
            <div class='grade-box'>
                <p>Your Score: <span class='grade-number'>{score}/{totalPoints}</span></p>
                <p>Percentage: {percentage}%</p>
                <p>Grade: <strong style='color: {gradeColor}; font-size: 20px;'>{gradeLetter}</strong></p>
            </div>
            <p>Log in to your account to see detailed feedback from your instructor.</p>
            <div class='footer'>
                <p>Best regards,<br/>The EduConnect Team</p>
            </div>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(studentEmail, subject, htmlContent);
        }

        /// <summary>
        /// Send announcement notification to multiple students
        /// </summary>
        public async Task SendAnnouncementAsync(List<string> studentEmails, string courseName, string announcementTitle, string announcementContent, string facultyName)
        {
            var subject = $"New Announcement in {courseName}: {announcementTitle}";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 5px 5px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border-radius: 0 0 5px 5px; }}
        .announcement-box {{ background: white; padding: 15px; border-left: 4px solid #667eea; margin: 15px 0; }}
        .button {{ background: #667eea; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block; margin-top: 15px; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #777; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📢 New Announcement</h1>
        </div>
        <div class='content'>
            <p>Dear Student,</p>
            <p>There is a new announcement in <strong>{courseName}</strong> from <strong>{facultyName}</strong>.</p>
            <div class='announcement-box'>
                <h3 style='margin-top: 0;'>{announcementTitle}</h3>
                <p>{announcementContent}</p>
            </div>
            <p>Log in to your account to see the full announcement and any attachments.</p>
            <a href='http://localhost:8000/Student/Dashboard' class='button'>View Announcement</a>
            <div class='footer'>
                <p>Best regards,<br/>The EduConnect Team</p>
                <p>Don't want to receive these emails? Update your notification preferences in your account settings.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            await SendBulkEmailAsync(studentEmails, subject, htmlContent);
        }

        /// <summary>
        /// Send welcome email to new user
        /// </summary>
        public async Task SendWelcomeEmailAsync(string email, string fullName, string role)
        {
            var subject = "Welcome to EduConnect!";
            var roleMessage = role switch
            {
                "Admin" => "You have been granted admin access to manage the platform.",
                "Faculty" => "You can now create courses, upload materials, and manage student progress.",
                "Student" => "Browse available courses, enroll, and start your learning journey!",
                _ => "Welcome to our learning platform!"
            };

            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 5px 5px 0 0; text-align: center; }}
        .content {{ background: #f9f9f9; padding: 20px; border-radius: 0 0 5px 5px; }}
        .button {{ background: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .features {{ background: white; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #777; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎓 Welcome to EduConnect, {fullName}!</h1>
        </div>
        <div class='content'>
            <p>Welcome! We're excited to have you join our learning community.</p>
            <p>{roleMessage}</p>
            <div class='features'>
                <h3>What You Can Do:</h3>
                <ul>
                    <li>Access your personalized dashboard</li>
                    <li>Manage your profile and settings</li>
                    <li>Explore courses and resources</li>
                    <li>Collaborate with peers</li>
                    <li>Track your progress</li>
                </ul>
            </div>
            <p><strong>Your Account Details:</strong></p>
            <p>Email: {email}<br/>Role: {role}</p>
            <a href='http://localhost:8000' class='button'>Log In to Your Account</a>
            <div class='footer'>
                <p>Best regards,<br/>The EduConnect Team</p>
                <p>If you have any questions, please contact our support team.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(email, subject, htmlContent);
        }

        /// <summary>
        /// Send password reset email
        /// </summary>
        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            var subject = "Reset Your EduConnect Password";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 5px 5px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border-radius: 0 0 5px 5px; }}
        .warning {{ background: #fff3cd; border: 1px solid #ffc107; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .button {{ background: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #777; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Reset Your Password</h1>
        </div>
        <div class='content'>
            <p>We received a request to reset your password. If you didn't make this request, please ignore this email.</p>
            <p>Click the button below to reset your password:</p>
            <a href='{resetLink}' class='button'>Reset Password</a>
            <p>Or copy this link: {resetLink}</p>
            <div class='warning'>
                <strong>⚠️ Security Notice:</strong> This link will expire in 24 hours for security reasons.
            </div>
            <div class='footer'>
                <p>Best regards,<br/>The EduConnect Team</p>
                <p>If you didn't request this, your account is still secure.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(email, subject, htmlContent);
        }

        /// <summary>
        /// Send course completion certificate
        /// </summary>
        public async Task SendCertificateEmailAsync(string studentEmail, string studentName, string courseName, DateTime completionDate)
        {
            var subject = $"Certificate Earned - {courseName}";
            var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 5px 5px 0 0; text-align: center; }}
        .content {{ background: #f9f9f9; padding: 20px; border-radius: 0 0 5px 5px; }}
        .certificate-box {{ background: white; border: 3px solid #ffc107; padding: 30px; border-radius: 10px; text-align: center; margin: 20px 0; }}
        .certificate-box h2 {{ color: #667eea; margin: 10px 0; }}
        .button {{ background: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 20px 0; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #777; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Congratulations!</h1>
        </div>
        <div class='content'>
            <p>Dear {studentName},</p>
            <p>Congratulations on completing <strong>{courseName}</strong>!</p>
            <div class='certificate-box'>
                <p style='font-style: italic; color: #666;'>This is to certify that</p>
                <h2>{studentName}</h2>
                <p style='color: #666;'>has successfully completed</p>
                <h3 style='color: #667eea;'>{courseName}</h3>
                <p style='color: #666;'>on {completionDate:MMMM dd, yyyy}</p>
            </div>
            <p>Your certificate is now available and can be downloaded from your dashboard or shared on professional networks like LinkedIn.</p>
            <a href='http://localhost:8000/Student/Dashboard' class='button'>Download Certificate</a>
            <div class='footer'>
                <p>Best regards,<br/>The EduConnect Team</p>
                <p>Share your achievement with your professional network!</p>
            </div>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(studentEmail, subject, htmlContent);
        }
    }
}
