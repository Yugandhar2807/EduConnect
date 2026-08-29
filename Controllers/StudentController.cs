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
        private readonly ILogger<StudentController> _logger;

        public StudentController(ApplicationDbContext context, IEmailService emailService, ILogger<StudentController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // ==================== DASHBOARD ====================

        /// <summary>Shortcut so /Student lands on the dashboard.</summary>
        public IActionResult Index() => RedirectToAction(nameof(Dashboard));

        public async Task<IActionResult> Dashboard()
        {
            var userId = CurrentUserId;
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

            var enrollments = await _context.Enrollments.AsNoTracking()
                .Where(e => e.StudentId == userId)
                .Include(e => e.Course)
                    .ThenInclude(c => c!.Faculty)
                .OrderByDescending(e => e.EnrolledAt)
                .ToListAsync();

            var announcements = await VisibleAnnouncementsQuery(userId!)
                .Take(5)
                .ToListAsync();

            var attendance = await _context.Attendances.AsNoTracking()
                .Where(a => a.StudentId == userId)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            var semesterResults = await _context.SemesterResults.AsNoTracking()
                .Where(sr => sr.StudentId == userId)
                .ToListAsync();

            var quizResults = await _context.QuizResults.AsNoTracking()
                .Where(r => r.StudentId == userId)
                .Include(r => r.Quiz)
                    .ThenInclude(q => q!.Course)
                .OrderByDescending(r => r.AttemptedAt)
                .ToListAsync();

            var model = new StudentDashboardViewModel
            {
                StudentName = user?.FullName ?? user?.FirstName ?? "Student",
                EnrolledCourses = enrollments.Count,
                CompletedCourses = enrollments.Count(e => e.IsCompleted),
                AttendancePercentage = attendance.Count > 0
                    ? Math.Round(attendance.Count(a => a.Status == "Present") * 100.0 / attendance.Count, 1)
                    : 0,
                AverageGpa = semesterResults.Count > 0 ? Math.Round((double)semesterResults.Average(sr => sr.GPA), 2) : null,
                AverageQuizScore = quizResults.Count > 0 ? Math.Round(quizResults.Average(r => r.PercentageScore), 1) : 0,
                Enrollments = enrollments,
                Announcements = announcements,
                RecentQuizResults = quizResults.Take(5).ToList(),
                RecentAttendance = attendance.Take(7).ToList(),
            };

            return View(model);
        }

        // ==================== COURSE CATALOG & ENROLLMENT ====================

        public async Task<IActionResult> BrowseCourses()
        {
            var userId = CurrentUserId;
            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == userId)
                .Select(e => e.CourseId)
                .ToListAsync();

            var courses = await _context.Courses.AsNoTracking()
                .Where(c => c.IsActive)
                .Include(c => c.Faculty)
                .Include(c => c.Enrollments)
                .Include(c => c.Topics)
                .Include(c => c.Quizzes)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.EnrolledCourseIds = enrolledCourseIds.ToHashSet();
            return View(courses);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollCourse(int courseId)
        {
            var userId = CurrentUserId;

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.IsActive);
            if (course == null)
            {
                TempData["Error"] = "This course is not available for enrollment.";
                return RedirectToAction(nameof(BrowseCourses));
            }

            var alreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseId == courseId && e.StudentId == userId);
            if (alreadyEnrolled)
            {
                TempData["Error"] = "You are already enrolled in this course.";
                return RedirectToAction(nameof(BrowseCourses));
            }

            _context.Add(new Enrollment
            {
                CourseId = courseId,
                StudentId = userId,
                EnrolledAt = DateTime.UtcNow,
            });
            await _context.SaveChangesAsync();

            try
            {
                var student = await _context.Users.FindAsync(userId);
                if (student?.Email != null)
                {
                    await _emailService.SendEnrollmentConfirmationAsync(
                        student.Email,
                        student.FullName ?? student.UserName!,
                        course.Title ?? "your new course");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Enrollment email failed for {UserId}", userId);
            }

            TempData["Success"] = $"You are now enrolled in '{course.Title}'. Happy learning!";
            return RedirectToAction(nameof(CourseDetails), new { id = courseId });
        }

        public async Task<IActionResult> CourseDetails(int? id)
        {
            if (id == null) return NotFound();
            var userId = CurrentUserId;

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == id && e.StudentId == userId);
            if (enrollment == null) return Forbid();

            var course = await _context.Courses.AsNoTracking()
                .Include(c => c.Faculty)
                .Include(c => c.Topics)
                .Include(c => c.Materials)
                .Include(c => c.Quizzes!)
                    .ThenInclude(q => q.Questions)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return NotFound();

            var completions = await _context.TopicProgress.AsNoTracking()
                .Where(tp => tp.StudentId == userId)
                .ToListAsync();

            var quizIds = course.Quizzes?.Select(q => q.Id).ToList() ?? new List<int>();
            var results = await _context.QuizResults.AsNoTracking()
                .Where(r => r.StudentId == userId && quizIds.Contains(r.QuizId))
                .ToListAsync();

            var model = new StudentCourseViewModel
            {
                Course = course,
                Enrollment = enrollment,
                CompletedTopicIds = completions.Where(c => c.TopicId.HasValue).Select(c => c.TopicId!.Value).ToHashSet(),
                CompletedMaterialIds = completions.Where(c => c.MaterialId.HasValue).Select(c => c.MaterialId!.Value).ToHashSet(),
                BestQuizResults = results
                    .GroupBy(r => r.QuizId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.PercentageScore).First()),
                QuizAttemptCounts = results
                    .GroupBy(r => r.QuizId)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ProgressPercentage = enrollment.ProgressPercentage,
            };

            return View(model);
        }

        // ==================== PROGRESS TRACKING ====================

        /// <summary>Marks a topic or material as complete and returns the updated course progress.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkComplete([FromQuery] int? topicId, [FromQuery] int? materialId, [FromQuery] int courseId)
        {
            if (!topicId.HasValue && !materialId.HasValue)
                return BadRequest("Either topicId or materialId must be provided.");

            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == userId);
            if (enrollment == null) return Forbid();

            if (topicId.HasValue)
            {
                var topicExists = await _context.Topics.AnyAsync(t => t.Id == topicId && t.CourseId == courseId);
                if (!topicExists) return NotFound("Topic not found.");

                var already = await _context.TopicProgress.AnyAsync(tp => tp.StudentId == userId && tp.TopicId == topicId);
                if (!already)
                    _context.Add(new TopicProgress { StudentId = userId, TopicId = topicId, CompletedAt = DateTime.UtcNow });
            }
            else
            {
                var materialExists = await _context.Materials.AnyAsync(m => m.Id == materialId && m.CourseId == courseId);
                if (!materialExists) return NotFound("Material not found.");

                var already = await _context.TopicProgress.AnyAsync(tp => tp.StudentId == userId && tp.MaterialId == materialId);
                if (!already)
                    _context.Add(new TopicProgress { StudentId = userId, MaterialId = materialId, CompletedAt = DateTime.UtcNow });
            }

            await _context.SaveChangesAsync();
            var progress = await RecalculateCourseProgressAsync(userId, courseId);

            return Ok(new { message = "Marked as complete", progress = Math.Round(progress, 1) });
        }

        public async Task<IActionResult> MyProgress()
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var viewModel = new StudentProgressViewModel
            {
                StudentId = userId,
                StudentName = user.FullName ?? user.UserName ?? "Student",
                Email = user.Email ?? string.Empty,
            };

            var attendances = await _context.Attendances.AsNoTracking()
                .Where(a => a.StudentId == userId)
                .ToListAsync();

            viewModel.TotalPresent = attendances.Count(a => a.Status == "Present");
            viewModel.TotalAbsent = attendances.Count(a => a.Status == "Absent");
            viewModel.TotalLeave = attendances.Count(a => a.Status == "Leave");
            viewModel.AttendancePercentage = attendances.Count > 0
                ? Math.Round(viewModel.TotalPresent * 100.0 / attendances.Count, 2)
                : 0;

            var courseProgresses = await _context.StudentCourseProgresses.AsNoTracking()
                .Where(cp => cp.StudentId == userId)
                .Include(cp => cp.Course)
                .ToListAsync();

            viewModel.ActiveCourses = courseProgresses.Count(cp => (double)cp.CompletionPercentage < 100);
            viewModel.CompletedCourses = courseProgresses.Count(cp => (double)cp.CompletionPercentage >= 100);

            foreach (var cp in courseProgresses)
            {
                viewModel.CourseProgressDetails.Add(new CourseProgressDetail
                {
                    CourseName = cp.Course?.Title ?? "Unknown Course",
                    CompletionPercentage = Math.Round((double)cp.CompletionPercentage, 2),
                    TopicsCompleted = cp.TopicsCompleted,
                    TotalTopics = cp.TotalTopics ?? 0,
                    QuizzesTaken = cp.QuizzesTaken,
                    AverageScore = Math.Round((double)cp.AverageScore, 2),
                    ProgressStatus = cp.ProgressStatus,
                });
            }
            viewModel.AverageCourseProgress = courseProgresses.Count > 0
                ? Math.Round(courseProgresses.Average(cp => (double)cp.CompletionPercentage), 2)
                : 0;

            var semesterResults = await _context.SemesterResults.AsNoTracking()
                .Where(sr => sr.StudentId == userId)
                .OrderBy(sr => sr.Semester)
                .ToListAsync();

            foreach (var sr in semesterResults)
            {
                viewModel.SemesterResultDetails.Add(new SemesterResultDetail
                {
                    Semester = sr.Semester,
                    CourseName = sr.CourseName,
                    MarksObtained = Math.Round((double)sr.MarksObtained, 2),
                    Grade = sr.Grade ?? "N/A",
                    GPA = Math.Round((double)sr.GPA, 2),
                });
            }
            viewModel.TotalSemesters = semesterResults.Select(sr => sr.Semester).Distinct().Count();
            viewModel.AverageGPA = semesterResults.Count > 0
                ? Math.Round(semesterResults.Average(sr => (double)sr.GPA), 2)
                : 0;

            viewModel.AttendanceBreakdown = attendances
                .GroupBy(a => new DateTime(a.AttendanceDate.Year, a.AttendanceDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new AttendanceBreakdownDetail
                {
                    Month = g.Key.ToString("MMM yyyy"),
                    Present = g.Count(a => a.Status == "Present"),
                    Absent = g.Count(a => a.Status == "Absent"),
                    Leave = g.Count(a => a.Status == "Leave"),
                })
                .ToList();

            return View(viewModel);
        }

        public async Task<IActionResult> MyAttendance()
        {
            var userId = CurrentUserId;

            var records = await _context.Attendances.AsNoTracking()
                .Where(a => a.StudentId == userId)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            var model = new StudentAttendanceViewModel
            {
                TotalPresent = records.Count(a => a.Status == "Present"),
                TotalAbsent = records.Count(a => a.Status == "Absent"),
                TotalLeave = records.Count(a => a.Status == "Leave"),
                AttendancePercentage = records.Count > 0
                    ? Math.Round(records.Count(a => a.Status == "Present") * 100.0 / records.Count, 1)
                    : 0,
                Records = records,
                MonthlyBreakdown = records
                    .GroupBy(a => new DateTime(a.AttendanceDate.Year, a.AttendanceDate.Month, 1))
                    .OrderBy(g => g.Key)
                    .Select(g => new AttendanceBreakdownDetail
                    {
                        Month = g.Key.ToString("MMM yyyy"),
                        Present = g.Count(a => a.Status == "Present"),
                        Absent = g.Count(a => a.Status == "Absent"),
                        Leave = g.Count(a => a.Status == "Leave"),
                    })
                    .ToList(),
            };
            return View(model);
        }

        public async Task<IActionResult> Announcements()
        {
            var userId = CurrentUserId;
            var announcements = await VisibleAnnouncementsQuery(userId!).ToListAsync();
            return View(new StudentAnnouncementsViewModel { Announcements = announcements });
        }

        // ==================== QUIZZES ====================

        public async Task<IActionResult> TakeQuiz(int? quizId)
        {
            if (quizId == null) return NotFound();
            var userId = CurrentUserId;

            var quiz = await _context.Quizzes.AsNoTracking()
                .Include(q => q.Questions)
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.Id == quizId && q.IsActive);
            if (quiz == null) return NotFound();

            var enrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseId == quiz.CourseId && e.StudentId == userId);
            if (!enrolled) return Forbid();

            if (quiz.Questions == null || quiz.Questions.Count == 0)
            {
                TempData["Error"] = "This quiz has no questions yet. Please check back later.";
                return RedirectToAction(nameof(CourseDetails), new { id = quiz.CourseId });
            }

            ViewBag.PreviousAttempts = await _context.QuizResults
                .CountAsync(r => r.QuizId == quizId && r.StudentId == userId);

            // Shuffle question order per attempt
            var rng = new Random();
            quiz.Questions = quiz.Questions.OrderBy(_ => rng.Next()).ToList();

            return View(quiz);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuiz([FromQuery] int quizId, [FromBody] Dictionary<string, string> answers)
        {
            if (answers == null) return BadRequest(new { success = false, error = "No answers provided" });

            var userId = CurrentUserId;

            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.Id == quizId);
            if (quiz == null) return NotFound();

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == quiz.CourseId && e.StudentId == userId);
            if (enrollment == null) return Forbid();

            if (quiz.Questions == null || quiz.Questions.Count == 0)
                return BadRequest(new { success = false, error = "This quiz has no questions. Please contact faculty." });

            var effectiveTotalMarks = quiz.TotalMarks > 0 ? quiz.TotalMarks : quiz.Questions.Sum(q => q.Marks);
            if (effectiveTotalMarks <= 0)
                return BadRequest(new { success = false, error = "Quiz total marks is invalid. Please contact faculty." });

            int marksObtained = 0;
            foreach (var question in quiz.Questions)
            {
                if (!answers.TryGetValue($"question_{question.Id}", out var studentAnswer))
                    continue;

                // Coding questions submit the program's output, graded against the
                // expected output; choice questions submit the selected option.
                var isCorrect = question.QuestionType == "Coding"
                    ? !string.IsNullOrWhiteSpace(question.ExpectedOutput) &&
                      string.Equals(studentAnswer?.Trim(), question.ExpectedOutput.Trim(), StringComparison.Ordinal)
                    : string.Equals(studentAnswer?.Trim(), question.CorrectOption?.Trim(), StringComparison.OrdinalIgnoreCase);

                if (isCorrect)
                    marksObtained += question.Marks;
            }

            var percentageScore = marksObtained * 100.0 / effectiveTotalMarks;
            var isPassed = percentageScore >= quiz.PassingMarks;

            int durationSeconds = 0;
            if (answers.TryGetValue("_durationSeconds", out var durationRaw))
                int.TryParse(durationRaw, out durationSeconds);

            var quizResult = new QuizResult
            {
                QuizId = quizId,
                StudentId = userId,
                MarksObtained = marksObtained,
                TotalMarks = effectiveTotalMarks,
                PercentageScore = Math.Round(percentageScore, 2),
                IsPassed = isPassed,
                AttemptedAt = DateTime.UtcNow,
                DurationTakenInSeconds = durationSeconds,
            };
            _context.Add(quizResult);
            await _context.SaveChangesAsync();

            await RecalculateCourseProgressAsync(userId!, quiz.CourseId);

            try
            {
                var student = await _context.Users.FindAsync(userId);
                if (student?.Email != null && quiz.Course != null)
                {
                    await _emailService.SendGradeNotificationAsync(
                        student.Email,
                        student.FullName ?? student.UserName!,
                        quiz.Course.Title ?? "",
                        quiz.Title ?? "",
                        marksObtained,
                        effectiveTotalMarks);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Grade notification email failed for {UserId}", userId);
            }

            return Json(new
            {
                success = true,
                marksObtained,
                totalMarks = effectiveTotalMarks,
                percentageScore = Math.Round(percentageScore, 2),
                isPassed,
                redirectUrl = Url.Action(nameof(QuizResult), new { quizResultId = quizResult.Id }),
            });
        }

        public async Task<IActionResult> QuizResult(int? quizResultId)
        {
            if (quizResultId == null) return NotFound();
            var userId = CurrentUserId;

            var result = await _context.QuizResults.AsNoTracking()
                .Where(r => r.Id == quizResultId && r.StudentId == userId)
                .Include(r => r.Quiz)
                    .ThenInclude(q => q!.Course)
                .Include(r => r.Quiz)
                    .ThenInclude(q => q!.Questions)
                .FirstOrDefaultAsync();
            if (result == null) return NotFound();

            return View(result);
        }

        public async Task<IActionResult> MyResults()
        {
            var userId = CurrentUserId;
            var results = await _context.QuizResults.AsNoTracking()
                .Where(r => r.StudentId == userId)
                .Include(r => r.Quiz)
                    .ThenInclude(q => q!.Course)
                .OrderByDescending(r => r.AttemptedAt)
                .ToListAsync();
            return View(results);
        }

        // ==================== ANALYTICS ====================

        public async Task<IActionResult> Analytics()
        {
            var userId = CurrentUserId;
            var student = await _context.Users.FindAsync(userId);
            if (student == null) return NotFound();

            return View(new StudentAnalyticsViewModel
            {
                StudentId = userId!,
                StudentName = student.FullName ?? student.UserName,
                Email = student.Email,
            });
        }

        /// <summary>JSON endpoint feeding the analytics dashboard charts.</summary>
        [HttpGet("api/student/analytics-data")]
        public async Task<IActionResult> GetAnalyticsData()
        {
            var userId = CurrentUserId;

            var enrollments = await _context.Enrollments.AsNoTracking()
                .Where(e => e.StudentId == userId)
                .Include(e => e.Course)
                .ToListAsync();

            var quizAverageByCourse = await _context.QuizResults.AsNoTracking()
                .Where(qr => qr.StudentId == userId)
                .GroupBy(qr => qr.Quiz!.CourseId)
                .Select(g => new { CourseId = g.Key, Avg = g.Average(x => x.PercentageScore) })
                .ToDictionaryAsync(x => x.CourseId, x => x.Avg);

            var courseProgress = await _context.StudentCourseProgresses.AsNoTracking()
                .Where(cp => cp.StudentId == userId)
                .Include(cp => cp.Course)
                .ToListAsync();

            var semesterResults = await _context.SemesterResults.AsNoTracking()
                .Where(sr => sr.StudentId == userId)
                .OrderBy(sr => sr.Semester)
                .ToListAsync();

            var attendance = await _context.Attendances.AsNoTracking()
                .Where(a => a.StudentId == userId)
                .OrderBy(a => a.AttendanceDate)
                .ToListAsync();

            return Json(new
            {
                enrollments = enrollments.Select(e => new
                {
                    e.CourseId,
                    CourseName = e.Course?.Title,
                    EnrollmentDate = e.EnrolledAt,
                    e.IsCompleted,
                    e.ProgressPercentage,
                    QuizAverageScore = quizAverageByCourse.TryGetValue(e.CourseId, out var avg) ? Math.Round(avg, 2) : 0,
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
                    cp.ProgressStatus,
                }),
                semesterResults = semesterResults.Select(sr => new
                {
                    sr.Semester,
                    sr.CourseName,
                    sr.MarksObtained,
                    sr.Grade,
                    sr.GPA,
                }),
                attendanceSummary = new
                {
                    TotalRecords = attendance.Count,
                    PresentDays = attendance.Count(a => a.Status == "Present"),
                    AbsentDays = attendance.Count(a => a.Status == "Absent"),
                    LeaveDays = attendance.Count(a => a.Status == "Leave"),
                    AttendancePercentage = attendance.Count > 0
                        ? Math.Round(attendance.Count(a => a.Status == "Present") * 100.0 / attendance.Count, 2)
                        : 0,
                },
                attendanceByMonth = attendance
                    .GroupBy(a => new DateTime(a.AttendanceDate.Year, a.AttendanceDate.Month, 1))
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Month = g.Key.ToString("MMM yyyy"),
                        Present = g.Count(a => a.Status == "Present"),
                        Absent = g.Count(a => a.Status == "Absent"),
                        Leave = g.Count(a => a.Status == "Leave"),
                    }),
            });
        }

        // ==================== CODE RUNNER (coding questions) ====================

        public class ExecuteCodeRequest
        {
            public string? Code { get; set; }
            public string? Language { get; set; }
        }

        /// <summary>
        /// Executes short student code snippets for coding questions.
        /// Local demo implementation — production deployments should use an isolated sandbox.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteCode([FromBody] ExecuteCodeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Code))
                return Json(new { success = false, error = "No code provided" });
            if (request.Code.Length > 10_000)
                return Json(new { success = false, error = "Code is too long (10,000 character limit)." });

            const int timeout = 5000;
            return request.Language?.ToLowerInvariant() switch
            {
                "python" => await ExecuteWithInterpreterAsync(FindExecutable("python", "python3"), request.Code, ".py", timeout),
                "javascript" => await ExecuteWithInterpreterAsync(FindExecutable("node"), request.Code, ".js", timeout),
                "csharp" => Json(new
                {
                    success = true,
                    output = "C# execution is not enabled on this server. Your code has been recorded — compare your logic against the expected output.",
                }),
                _ => Json(new { success = false, error = "Unsupported language" }),
            };
        }

        private async Task<IActionResult> ExecuteWithInterpreterAsync(string? interpreterPath, string code, string extension, int timeout)
        {
            if (string.IsNullOrEmpty(interpreterPath))
            {
                return Json(new
                {
                    success = true,
                    output = "The runtime for this language is not installed on the server. Verify your code logic against the expected output shown in the question.",
                });
            }

            var tempFile = Path.Combine(Path.GetTempPath(), $"educonnect_{Guid.NewGuid()}{extension}");
            await System.IO.File.WriteAllTextAsync(tempFile, code);

            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = interpreterPath,
                        Arguments = $"\"{tempFile}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    },
                };

                process.Start();
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                if (!process.WaitForExit(timeout))
                {
                    process.Kill(entireProcessTree: true);
                    return Json(new { success = false, error = "Execution timeout (5 seconds)" });
                }

                var output = await outputTask;
                var error = await errorTask;

                if (!string.IsNullOrEmpty(error))
                    return Json(new { success = false, error });

                return Json(new { success = true, output = output.Trim() });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
            finally
            {
                if (System.IO.File.Exists(tempFile))
                    System.IO.File.Delete(tempFile);
            }
        }

        private static string? FindExecutable(params string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                try
                {
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = candidate,
                            Arguments = "--version",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        },
                    };
                    process.Start();
                    if (process.WaitForExit(1500) && process.ExitCode == 0)
                        return candidate;
                }
                catch
                {
                    // Not available — try the next candidate.
                }
            }
            return null;
        }

        // ==================== HELPERS ====================

        private IQueryable<Announcement> VisibleAnnouncementsQuery(string userId)
        {
            return _context.Announcements.AsNoTracking()
                .Where(a => a.IsActive && (a.CourseId == null ||
                    a.Course!.Enrollments!.Any(e => e.StudentId == userId)))
                .Include(a => a.Course)
                .Include(a => a.Faculty)
                .OrderByDescending(a => a.CreatedAt);
        }

        /// <summary>
        /// Recomputes course progress (15% topics + 25% materials + 60% quizzes), then
        /// updates both the enrollment and the StudentCourseProgress record.
        /// </summary>
        private async Task<double> RecalculateCourseProgressAsync(string userId, int courseId)
        {
            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == userId);
            if (enrollment == null) return 0;

            var course = await _context.Courses.AsNoTracking()
                .Include(c => c.Topics)
                .Include(c => c.Materials)
                .Include(c => c.Quizzes)
                .FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null) return 0;

            var topicIds = course.Topics?.Select(t => t.Id).ToList() ?? new List<int>();
            var materialIds = course.Materials?.Select(m => m.Id).ToList() ?? new List<int>();
            var quizIds = course.Quizzes?.Select(q => q.Id).ToList() ?? new List<int>();

            var completedTopics = topicIds.Count > 0
                ? await _context.TopicProgress
                    .Where(tp => tp.StudentId == userId && tp.TopicId.HasValue && topicIds.Contains(tp.TopicId.Value))
                    .Select(tp => tp.TopicId).Distinct().CountAsync()
                : 0;
            var completedMaterials = materialIds.Count > 0
                ? await _context.TopicProgress
                    .Where(tp => tp.StudentId == userId && tp.MaterialId.HasValue && materialIds.Contains(tp.MaterialId.Value))
                    .Select(tp => tp.MaterialId).Distinct().CountAsync()
                : 0;

            var myResults = quizIds.Count > 0
                ? await _context.QuizResults
                    .Where(r => r.StudentId == userId && quizIds.Contains(r.QuizId))
                    .ToListAsync()
                : new List<QuizResult>();
            var quizzesAttempted = myResults.Select(r => r.QuizId).Distinct().Count();

            double topicsPart = topicIds.Count > 0 ? completedTopics / (double)topicIds.Count * 15 : 0;
            double materialsPart = materialIds.Count > 0 ? completedMaterials / (double)materialIds.Count * 25 : 0;
            double quizzesPart = quizIds.Count > 0 ? quizzesAttempted / (double)quizIds.Count * 60 : 0;
            var totalProgress = Math.Min(topicsPart + materialsPart + quizzesPart, 100);

            enrollment.ProgressPercentage = (int)Math.Round(totalProgress);
            enrollment.IsCompleted = totalProgress >= 100;

            var progressRow = await _context.StudentCourseProgresses
                .FirstOrDefaultAsync(cp => cp.StudentId == userId && cp.CourseId == courseId);
            if (progressRow == null)
            {
                progressRow = new StudentCourseProgress
                {
                    StudentId = userId,
                    CourseId = courseId,
                    EnrollmentDate = enrollment.EnrolledAt,
                    ProgressStatus = "In Progress",
                };
                _context.StudentCourseProgresses.Add(progressRow);
            }

            progressRow.TopicsCompleted = completedTopics;
            progressRow.TotalTopics = topicIds.Count;
            progressRow.CompletionPercentage = (decimal)Math.Round(totalProgress, 2);
            progressRow.QuizzesTaken = quizzesAttempted;
            progressRow.AverageScore = myResults.Count > 0 ? (decimal)Math.Round(myResults.Average(r => r.PercentageScore), 2) : 0;
            progressRow.ProgressStatus = totalProgress >= 100 ? "Completed" : totalProgress > 0 ? "In Progress" : "Not Started";
            progressRow.LastActivityDate = DateTime.UtcNow;
            progressRow.CompletedAt = totalProgress >= 100 ? (progressRow.CompletedAt ?? DateTime.UtcNow) : null;

            await _context.SaveChangesAsync();
            return totalProgress;
        }
    }
}
