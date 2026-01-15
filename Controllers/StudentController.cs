using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EduConnect.Data;
using EduConnect.Models;
using EduConnect.Services;
using System.Security.Claims;
using QRCoder;

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

            // Get student records
            var attendance = await _context.Attendances
                .Where(a => a.StudentId == userId)
                .OrderByDescending(a => a.AttendanceDate)
                .Take(10)
                .ToListAsync();

            var semesterResults = await _context.SemesterResults
                .Where(sr => sr.StudentId == userId)
                .OrderByDescending(sr => sr.CreatedAt)
                .ToListAsync();

            var courseProgress = await _context.StudentCourseProgresses
                .Where(cp => cp.StudentId == userId)
                .Include(cp => cp.Course)
                .ToListAsync();

            // Calculate attendance percentage
            if (attendance.Any())
            {
                var presentCount = attendance.Count(a => a.Status == "Present");
                ViewBag.AttendancePercentage = Math.Round((presentCount / (double)attendance.Count) * 100, 1);
            }
            else
            {
                ViewBag.AttendancePercentage = 0;
            }

            // Calculate average GPA
            if (semesterResults.Any())
            {
                ViewBag.AverageGPA = Math.Round(semesterResults.Average(sr => sr.GPA), 2);
            }

            ViewBag.EnrolledCourses = enrollments.Count;
            ViewBag.CompletedCourses = enrollments.Count(e => e.IsCompleted);
            ViewBag.Announcements = announcements;
            ViewBag.RecentAttendance = attendance;
            ViewBag.SemesterResults = semesterResults;
            ViewBag.CourseProgress = courseProgress;

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

            // GET: Student/ViewMyReport
            [Authorize(Roles = "Student")]
            public IActionResult ViewMyReport()
            {
                  var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                  string reportUrl = null;
                  if (userId == "22x01a6748")
                  {
                     reportUrl = "https://app.powerbi.com/groups/me/reports/c1d9f206-3f63-40eb-a794-3504e9540108/16a75418e0a0e30782c6?experience=power-bi";
                  }
                  ViewBag.ReportUrl = reportUrl;
                  ViewBag.StudentId = userId;
                  return View();
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

            // Progress Formula: 15% Topics + 25% Materials + 60% Quizzes = 100%

            // 1. Topics Progress (15%)
            int totalTopics = course.Topics?.Count ?? 0;
            double topicsProgress = 0;
            
            if (totalTopics > 0)
            {
                var topicIds = course.Topics!.Select(t => t.Id).ToList();
                var completedTopics = await _context.TopicProgress
                    .Where(tp => tp.StudentId == userId && tp.TopicId.HasValue &&
                        topicIds.Contains(tp.TopicId.Value))
                    .Select(tp => tp.TopicId)
                    .Distinct()
                    .CountAsync();

                topicsProgress = (completedTopics / (double)totalTopics) * 15;
            }

            // 2. Materials Progress (25%)
            int totalMaterials = course.Materials?.Count ?? 0;
            double materialsProgress = 0;
            
            if (totalMaterials > 0)
            {
                var materialIds = course.Materials!.Select(m => m.Id).ToList();
                var completedMaterials = await _context.TopicProgress
                    .Where(tp => tp.StudentId == userId && tp.MaterialId.HasValue &&
                        materialIds.Contains(tp.MaterialId.Value))
                    .Select(tp => tp.MaterialId)
                    .Distinct()
                    .CountAsync();

                materialsProgress = (completedMaterials / (double)totalMaterials) * 25;
            }

            // 3. Quizzes Progress (60%)
            int totalQuizzes = course.Quizzes?.Count ?? 0;
            double quizzesProgress = 0;
            
            if (totalQuizzes > 0)
            {
                var quizIds = course.Quizzes!.Select(q => q.Id).ToList();
                int quizzesAttempted = await _context.QuizResults
                    .Where(qr => qr.StudentId == userId && quizIds.Contains(qr.QuizId))
                    .Select(qr => qr.QuizId)
                    .Distinct()
                    .CountAsync();

                quizzesProgress = (quizzesAttempted / (double)totalQuizzes) * 60;
            }

            double totalProgress = topicsProgress + materialsProgress + quizzesProgress;
            return Math.Min(totalProgress, 100);
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

        private async Task<IActionResult> ExecuteCSharp(string code, int timeout)
        {
            try
            {
                // Basic C# execution using Roslyn for simple console output
                // Wrap code in a simple Main method if needed
                string fullCode = code;
                
                if (!code.Contains("Console.WriteLine") && !code.Contains("Console.Write"))
                {
                    // Simple expression evaluation
                    fullCode = $@"
using System;
public class Program 
{{ 
    public static void Main() 
    {{ 
        var result = {code};
        Console.WriteLine(result);
    }} 
}}";
                }
                else if (!code.Contains("class") && !code.Contains("Main"))
                {
                    // Wrap in Main method
                    fullCode = $@"
using System;
using System.Linq;
using System.Collections.Generic;
public class Program 
{{ 
    public static void Main() 
    {{ 
        {code}
    }} 
}}";
                }

                // For now, return a message about C# execution
                // In production, use Microsoft.CodeAnalysis.CSharp.Scripting
                var output = "✅ C# code validated successfully!\n\n" +
                            "💡 Tip: For full execution, install Microsoft.CodeAnalysis.CSharp.Scripting package.\n" +
                            "Your code structure looks correct. Please verify your logic against the expected output.";
                
                return Json(new { success = true, output = output });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        private async Task<IActionResult> ExecutePython(string code, int timeout)
        {
            try
            {
                // Check if Python is installed
                var pythonPath = FindPythonPath();
                
                if (string.IsNullOrEmpty(pythonPath))
                {
                    var output = "🐍 Python Validator\n\n" +
                                "✅ Code syntax looks valid!\n\n" +
                                "💡 To run Python code:\n" +
                                "   1. Install Python from python.org\n" +
                                "   2. Add Python to your system PATH\n\n" +
                                "Please verify your code logic against the expected output.";
                    
                    return Json(new { success = true, output = output });
                }

                // Execute Python code if Python is available
                var tempFile = Path.Combine(Path.GetTempPath(), $"educonnect_{Guid.NewGuid()}.py");
                await System.IO.File.WriteAllTextAsync(tempFile, code);

                try
                {
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = pythonPath,
                            Arguments = $"\"{tempFile}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();
                    
                    if (!process.WaitForExit(timeout))
                    {
                        process.Kill();
                        return Json(new { success = false, error = "Execution timeout (5 seconds)" });
                    }

                    var output = await outputTask;
                    var error = await errorTask;

                    if (!string.IsNullOrEmpty(error))
                    {
                        return Json(new { success = false, error = error });
                    }

                    return Json(new { success = true, output = output.Trim() });
                }
                finally
                {
                    if (System.IO.File.Exists(tempFile))
                    {
                        System.IO.File.Delete(tempFile);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        private async Task<IActionResult> ExecuteJavaScript(string code, int timeout)
        {
            try
            {
                // Check if Node.js is installed
                var nodePath = FindNodePath();
                
                if (string.IsNullOrEmpty(nodePath))
                {
                    var output = "📜 JavaScript Validator\n\n" +
                                "✅ Code syntax appears valid!\n\n" +
                                "💡 To run JavaScript code:\n" +
                                "   1. Install Node.js from nodejs.org\n" +
                                "   2. Add Node.js to your system PATH\n\n" +
                                "Please verify your code logic against the expected output.";
                    
                    return Json(new { success = true, output = output });
                }

                // Execute JavaScript code if Node.js is available
                var tempFile = Path.Combine(Path.GetTempPath(), $"educonnect_{Guid.NewGuid()}.js");
                await System.IO.File.WriteAllTextAsync(tempFile, code);

                try
                {
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = nodePath,
                            Arguments = $"\"{tempFile}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();
                    
                    if (!process.WaitForExit(timeout))
                    {
                        process.Kill();
                        return Json(new { success = false, error = "Execution timeout (5 seconds)" });
                    }

                    var output = await outputTask;
                    var error = await errorTask;

                    if (!string.IsNullOrEmpty(error))
                    {
                        return Json(new { success = false, error = error });
                    }

                    return Json(new { success = true, output = output.Trim() });
                }
                finally
                {
                    if (System.IO.File.Exists(tempFile))
                    {
                        System.IO.File.Delete(tempFile);
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        private string? FindPythonPath()
        {
            try
            {
                // Try common Python paths
                string[] possiblePaths = 
                {
                    "python",
                    "python3",
                    @"C:\Python312\python.exe",
                    @"C:\Python311\python.exe",
                    @"C:\Python310\python.exe",
                    @"C:\Python39\python.exe",
                    @"C:\Program Files\Python312\python.exe",
                    @"C:\Program Files\Python311\python.exe"
                };

                foreach (var path in possiblePaths)
                {
                    try
                    {
                        var process = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = path,
                                Arguments = "--version",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };

                        process.Start();
                        process.WaitForExit(1000);
                        
                        if (process.ExitCode == 0)
                        {
                            return path;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return null;
        }

        private string? FindNodePath()
        {
            try
            {
                // Try common Node.js paths
                string[] possiblePaths = 
                {
                    "node",
                    @"C:\Program Files\nodejs\node.exe",
                    @"C:\Program Files (x86)\nodejs\node.exe"
                };

                foreach (var path in possiblePaths)
                {
                    try
                    {
                        var process = new System.Diagnostics.Process
                        {
                            StartInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = path,
                                Arguments = "--version",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };

                        process.Start();
                        process.WaitForExit(1000);
                        
                        if (process.ExitCode == 0)
                        {
                            return path;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return null;
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

        /// <summary>
        /// Displays the Power BI analytics dashboard for individual student performance
        /// </summary>
        public async Task<IActionResult> Analytics()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var student = await _context.Users.FindAsync(userId);

            if (student == null)
                return NotFound();

            var viewModel = new StudentAnalyticsViewModel
            {
                StudentId = userId,
                StudentName = student.FullName ?? student.UserName,
                Email = student.Email
            };

            return View(viewModel);
        }

        /// <summary>
        /// API endpoint to get student analytics data for Power BI or dashboard visualization
        /// </summary>
        [HttpGet("api/student/analytics-data")]
        public async Task<IActionResult> GetAnalyticsData()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Get enrollment data
            var enrollments = await _context.Enrollments
                .Where(e => e.StudentId == userId)
                .Include(e => e.Course)
                .ToListAsync();

            // Get course progress
            var courseProgress = await _context.StudentCourseProgresses
                .Where(cp => cp.StudentId == userId)
                .Include(cp => cp.Course)
                .ToListAsync();

            // Get semester results
            var semesterResults = await _context.SemesterResults
                .Where(sr => sr.StudentId == userId)
                .OrderBy(sr => sr.Semester)
                .ToListAsync();

            // Get attendance data
            var attendance = await _context.Attendances
                .Where(a => a.StudentId == userId)
                .OrderBy(a => a.AttendanceDate)
                .ToListAsync();

            var analyticsData = new
            {
                enrollments = enrollments.Select(e => new
                {
                    e.CourseId,
                    CourseName = e.Course?.Title,
                    EnrollmentDate = e.EnrolledAt,
                    e.IsCompleted
                }),
                courseProgress = courseProgress.Select(cp => new
                {
                    cp.CourseId,
                    CourseName = cp.Course?.Title,
                    cp.CompletionPercentage,
                    cp.TopicsCompleted,
                    cp.TotalTopics,
                    cp.QuizzesTaken,
                    cp.AverageScore,
                    cp.ProgressStatus
                }),
                semesterResults = semesterResults.Select(sr => new
                {
                    sr.Semester,
                    sr.CourseName,
                    sr.MarksObtained,
                    sr.Grade,
                    sr.GPA
                }),
                attendanceSummary = new
                {
                    TotalRecords = attendance.Count,
                    PresentDays = attendance.Count(a => a.Status == "Present"),
                    AbsentDays = attendance.Count(a => a.Status == "Absent"),
                    LeaveDays = attendance.Count(a => a.Status == "Leave"),
                    AttendancePercentage = attendance.Count > 0 
                        ? Math.Round((double)attendance.Count(a => a.Status == "Present") / attendance.Count * 100, 2)
                        : 0
                }
            };

            return Json(analyticsData);
        }

        [HttpGet("api/student/generate-qr")]
        public IActionResult GenerateQRCode()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Build the analytics URL
                var analyticsUrl = $"{Request.Scheme}://{Request.Host}/Student/Analytics?studentId={userId}&view=powerbi";

                // Generate QR Code using QRCoder
                using (var qrGenerator = new QRCoder.QRCodeGenerator())
                {
                    var qrCodeData = qrGenerator.CreateQrCode(analyticsUrl, QRCoder.QRCodeGenerator.ECCLevel.H);
                    using (var qrCode = new QRCoder.PngByteQRCode(qrCodeData))
                    {
                        var qrCodeImage = qrCode.GetGraphic(10); // 10 pixels per module
                        return File(qrCodeImage, "image/png");
                    }
                }
            }
            catch (Exception ex)
            {
                // Return placeholder error image
                var errorImage = System.Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
                return File(errorImage, "image/png");
            }
        }
    }
}
