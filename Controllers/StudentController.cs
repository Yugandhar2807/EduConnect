using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EduConnect.Data;
using EduConnect.Models;
using EduConnect.Services;
using System.Security.Claims;

namespace EduConnect.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public StudentController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var enrollments = await _context.Enrollments
                .Where(e => e.StudentId == userId)
                .Include(e => e.Course)
                .ToListAsync();

            var announcements = await _context.Announcements
                .Where(a => a.IsActive && (a.CourseId == null || 
                    (a.Course != null && a.Course.Enrollments != null && 
                    a.Course.Enrollments.Any(e => e.StudentId == userId))))
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.EnrolledCourses = enrollments.Count;
            ViewBag.CompletedCourses = enrollments.Count(e => e.IsCompleted);

            ViewBag.Announcements = announcements;

            return View(enrollments);
        }

        public async Task<IActionResult> BrowseCourses()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == userId)
                .Select(e => e.CourseId)
                .ToListAsync();

            var courses = await _context.Courses
                .Where(c => c.IsActive && !enrolledCourseIds.Contains(c.Id))
                .Include(c => c.Faculty)
                .ToListAsync();

            return View(courses);
        }

        [HttpPost]
        public async Task<IActionResult> EnrollCourse(int courseId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var existingEnrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == userId);

            if (existingEnrollment != null)
            {
                TempData["Error"] = "You are already enrolled in this course.";
                return RedirectToAction(nameof(BrowseCourses));
            }

            var enrollment = new Enrollment
            {
                CourseId = courseId,
                StudentId = userId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.Add(enrollment);
            await _context.SaveChangesAsync();

            // Send enrollment confirmation email
            var student = await _context.Users.FindAsync(userId);
            var course = await _context.Courses.FindAsync(courseId);
            
            if (student != null && course != null)
            {
                await _emailService.SendEnrollmentConfirmationAsync(
                    student.Email!,
                    student.FullName ?? student.UserName!,
                    course.Title
                );
            }

            TempData["Success"] = "Successfully enrolled in the course! Check your email for confirmation.";
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> CourseDetails(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == id && e.StudentId == userId);

            if (enrollment == null) return Forbid();

            var course = await _context.Courses
                .Include(c => c.Topics)
                .Include(c => c.Materials)
                .Include(c => c.Quizzes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            return View(course);
        }

        public async Task<IActionResult> MyProgress()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var enrollments = await _context.Enrollments
                .Where(e => e.StudentId == userId)
                .Include(e => e.Course)
                .ThenInclude(c => c!.Topics)
                .Include(e => e.Course)
                .ThenInclude(c => c!.Materials)
                .Include(e => e.Course)
                .ThenInclude(c => c!.Quizzes)
                .ToListAsync();

            // Calculate progress for each enrollment
            foreach (var enrollment in enrollments)
            {
                if (enrollment.Course != null)
                {
                    enrollment.ProgressPercentage = (int)await CalculateCourseProgress(userId, enrollment.CourseId);
                }
            }

            var results = await _context.QuizResults
                .Where(r => r.StudentId == userId)
                .Include(r => r.Quiz)
                .ThenInclude(q => q!.Course)
                .ToListAsync();

            ViewBag.QuizResults = results;

            return View(enrollments);
        }

        public async Task<IActionResult> TakeQuiz(int? quizId)
        {
            if (quizId == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null) return NotFound();

            // Verify student is enrolled in the course
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == quiz.CourseId && e.StudentId == userId);

            if (enrollment == null) return Forbid();

            // Randomize question order for security
            var random = new Random();
            quiz.Questions = quiz.Questions?.OrderBy(_ => random.Next()).ToList();

            return View(quiz);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitQuiz([FromQuery] int quizId, [FromBody] Dictionary<string, string> answers)
        {
            if (answers == null) return BadRequest("No answers provided");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.Id == quizId);

            if (quiz == null) return NotFound();

            // Verify student is enrolled
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == quiz.CourseId && e.StudentId == userId);

            if (enrollment == null) return Forbid();

            // Calculate score
            int marksObtained = 0;
            foreach (var question in quiz.Questions!)
            {
                if (answers.TryGetValue($"question_{question.Id}", out var studentAnswer))
                {
                    if (studentAnswer.ToString().ToUpper() == question.CorrectOption.ToString().ToUpper())
                    {
                        marksObtained += question.Marks;
                    }
                }
            }

            var percentageScore = (marksObtained / (double)quiz.TotalMarks) * 100;
            var isPassed = percentageScore >= quiz.PassingMarks;

            var quizResult = new QuizResult
            {
                QuizId = quizId,
                StudentId = userId,
                MarksObtained = marksObtained,
                TotalMarks = quiz.TotalMarks,
                PercentageScore = percentageScore,
                IsPassed = isPassed,
                AttemptedAt = DateTime.UtcNow
            };

            _context.Add(quizResult);

            // Update enrollment progress
            var allCourseQuizResults = await _context.QuizResults
                .Where(r => r.Quiz!.CourseId == quiz.CourseId && r.StudentId == userId)
                .ToListAsync();

            var materials = await _context.Materials
                .Where(m => m.CourseId == quiz.CourseId)
                .ToListAsync();

            var quizzes = await _context.Quizzes
                .Where(q => q.CourseId == quiz.CourseId)
                .ToListAsync();

            // Calculate progress: (quiz_average + materials_count) / (total_items)
            double avgQuizScore = allCourseQuizResults.Any() 
                ? allCourseQuizResults.Average(r => r.PercentageScore) 
                : 0;

            int totalItems = Math.Max(materials.Count, 1) + Math.Max(quizzes.Count, 1);
            double progressPercentage = ((avgQuizScore / 100) * quizzes.Count + materials.Count) / totalItems * 100;

            enrollment.ProgressPercentage = (int)progressPercentage;
            enrollment.IsCompleted = progressPercentage >= 100;

            _context.Update(enrollment);
            await _context.SaveChangesAsync();

            // Send grade notification email
            var student = await _context.Users.FindAsync(userId);
            if (student != null && quiz.Course != null)
            {
                await _emailService.SendGradeNotificationAsync(
                    student.Email!,
                    student.FullName ?? student.UserName!,
                    quiz.Course.Title,
                    quiz.Title,
                    marksObtained,
                    quiz.TotalMarks
                );
            }

            return Json(new { 
                success = true, 
                marksObtained, 
                totalMarks = quiz.TotalMarks,
                percentageScore = Math.Round(percentageScore, 2),
                isPassed,
                redirectUrl = Url.Action(nameof(QuizResult), new { quizResultId = quizResult.Id })
            });
        }

        public async Task<IActionResult> QuizResult(int? quizResultId)
        {
            if (quizResultId == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var result = await _context.QuizResults
                .Where(r => r.Id == quizResultId && r.StudentId == userId)
                .Include(r => r.Quiz)
                .ThenInclude(q => q!.Course)
                .Include(r => r.Quiz)
                .ThenInclude(q => q!.Questions)
                .FirstOrDefaultAsync();

            if (result == null) return NotFound();

            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> CompleteCourse(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.Id == id && e.StudentId == userId);

            if (enrollment == null) return NotFound();

            enrollment.IsCompleted = true;
            _context.Update(enrollment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Course marked as completed! Congratulations! 🎉";
            return RedirectToAction(nameof(MyProgress));
        }

        /// <summary>
        /// Mark a topic or material as complete
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> MarkComplete([FromQuery] int? topicId, [FromQuery] int? materialId, [FromQuery] int courseId)
        {
            if (!topicId.HasValue && !materialId.HasValue)
                return BadRequest("Either topicId or materialId must be provided.");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // Verify enrollment
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == userId);

            if (enrollment == null)
                return Forbid("You are not enrolled in this course.");

            TopicProgress? progress = null;

            if (topicId.HasValue)
            {
                // Check if topic exists and belongs to the course
                var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Id == topicId && t.CourseId == courseId);
                if (topic == null)
                    return NotFound("Topic not found.");

                // Check if already completed
                var existing = await _context.TopicProgress
                    .FirstOrDefaultAsync(tp => tp.StudentId == userId && tp.TopicId == topicId);

                if (existing != null)
                    return Ok(new { message = "Already completed", isNew = false });

                progress = new TopicProgress
                {
                    StudentId = userId,
                    TopicId = topicId,
                    CompletedAt = DateTime.UtcNow
                };
            }
            else if (materialId.HasValue)
            {
                // Check if material exists and belongs to the course
                var material = await _context.Materials.FirstOrDefaultAsync(m => m.Id == materialId && m.CourseId == courseId);
                if (material == null)
                    return NotFound("Material not found.");

                // Check if already completed
                var existing = await _context.TopicProgress
                    .FirstOrDefaultAsync(tp => tp.StudentId == userId && tp.MaterialId == materialId);

                if (existing != null)
                    return Ok(new { message = "Already completed", isNew = false });

                progress = new TopicProgress
                {
                    StudentId = userId,
                    MaterialId = materialId,
                    CompletedAt = DateTime.UtcNow
                };
            }

            if (progress != null)
            {
                _context.Add(progress);
                await _context.SaveChangesAsync();
            }

            // Calculate progress
            var courseProgress = await CalculateCourseProgress(userId, courseId);

            return Ok(new { message = "Marked as complete", isNew = true, progress = courseProgress });
        }

        /// <summary>
        /// Calculate overall progress for a course (50% topics/materials + 50% quizzes)
        /// </summary>
        private async Task<double> CalculateCourseProgress(string userId, int courseId)
        {
            var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == userId);
            if (enrollment == null) return 0;

            var course = await _context.Courses
                .Include(c => c.Topics)
                .Include(c => c.Materials)
                .Include(c => c.Quizzes)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) return 0;

            // Count total topics + materials
            int totalTopics = course.Topics?.Count ?? 0;
            int totalMaterials = course.Materials?.Count ?? 0;
            int totalTopicsAndMaterials = totalTopics + totalMaterials;

            if (totalTopicsAndMaterials == 0)
            {
                // If no topics/materials, progress is 100% if they've attempted any quiz, else based on quizzes
                totalTopicsAndMaterials = 1; // Avoid division by zero
            }

            // Count completed topics + materials
            var completedItems = await _context.TopicProgress
                .Where(tp => tp.StudentId == userId && 
                    (tp.TopicId.HasValue || tp.MaterialId.HasValue))
                .ToListAsync();

            int completedCount = completedItems.Count(tp =>
                (tp.TopicId.HasValue && course.Topics != null && course.Topics.Any(t => t.Id == tp.TopicId)) ||
                (tp.MaterialId.HasValue && course.Materials != null && course.Materials.Any(m => m.Id == tp.MaterialId)));

            double topicsProgress = (totalTopicsAndMaterials > 0) ? (completedCount / (double)totalTopicsAndMaterials) * 50 : 0;

            // Count quizzes attempted (50% of progress)
            int totalQuizzes = course.Quizzes?.Count ?? 0;
            if (totalQuizzes == 0)
            {
                totalQuizzes = 1; // Avoid division by zero
            }

            var quizIds = course.Quizzes?.Select(q => q.Id).ToList() ?? new List<int>();
            int quizzesAttempted = await _context.QuizResults
                .Where(qr => qr.StudentId == userId && quizIds.Contains(qr.QuizId))
                .Select(qr => qr.QuizId)
                .Distinct()
                .CountAsync();

            double quizProgress = (totalQuizzes > 0) ? (quizzesAttempted / (double)totalQuizzes) * 50 : 0;

            return Math.Min(topicsProgress + quizProgress, 100);
        }

        /// <summary>
        /// Execute student code for coding questions (basic implementation)
        /// This is a simplified version for demonstration purposes
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ExecuteCode([FromBody] dynamic request)
        {
            try
            {
                string? code = request?.code;
                string? language = request?.language;

                if (string.IsNullOrEmpty(code))
                    return Json(new { success = false, error = "No code provided" });

                // For this simple implementation, we'll use a basic approach
                // In production, use a proper sandboxing solution like Docker or a code execution API
                
                // Timeout in milliseconds
                int timeout = 5000;
                
                switch (language?.ToLower())
                {
                    case "csharp":
                        return await ExecuteCSharp(code, timeout);
                    case "python":
                        return await ExecutePython(code, timeout);
                    case "javascript":
                        return await ExecuteJavaScript(code, timeout);
                    default:
                        return Json(new { success = false, error = "Unsupported language" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        private Task<IActionResult> ExecuteCSharp(string code, int timeout)
        {
            try
            {
                // Simple C# evaluation for basic expressions/console output
                // For production, use Roslyn or a proper C# REPL service
                
                // This is a placeholder - actual implementation would use Roslyn compiler
                var output = "C# code execution not yet implemented in demo. " +
                            "Please check the expected output to verify your code logic.";
                
                return Task.FromResult((IActionResult)Json(new { success = true, output = output }));
            }
            catch (Exception ex)
            {
                return Task.FromResult((IActionResult)Json(new { success = false, error = ex.Message }));
            }
        }

        private Task<IActionResult> ExecutePython(string code, int timeout)
        {
            try
            {
                // For Python execution, you could use:
                // 1. Python.NET (pythonnet)
                // 2. External Python process
                // 3. API call to a Python execution service
                
                var output = "Python code execution not yet implemented in demo. " +
                            "Please check the expected output to verify your code logic.";
                
                return Task.FromResult((IActionResult)Json(new { success = true, output = output }));
            }
            catch (Exception ex)
            {
                return Task.FromResult((IActionResult)Json(new { success = false, error = ex.Message }));
            }
        }

        private Task<IActionResult> ExecuteJavaScript(string code, int timeout)
        {
            try
            {
                // For JavaScript execution, you could use:
                // 1. Jint (JavaScript interpreter for .NET)
                // 2. Node.js via external process
                // 3. API call to a JS execution service
                
                var output = "JavaScript code execution not yet implemented in demo. " +
                            "Please check the expected output to verify your code logic.";
                
                return Task.FromResult((IActionResult)Json(new { success = true, output = output }));
            }
            catch (Exception ex)
            {
                return Task.FromResult((IActionResult)Json(new { success = false, error = ex.Message }));
            }
        }

        // TEST EMAIL ENDPOINT
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> TestEmail()
        {
            try
            {
                var testEmail = "test@example.com";
                var result = await _emailService.SendEmailAsync(
                    testEmail,
                    "Test Email from EduConnect",
                    "<h1>Test Email</h1><p>If you received this, the email service is working!</p>"
                );

                return Json(new { 
                    success = result, 
                    message = result ? "Email sent successfully" : "Failed to send email",
                    testEmail = testEmail
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}
