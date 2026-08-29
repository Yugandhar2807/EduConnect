using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EduConnect.Data;
using EduConnect.Models;
using EduConnect.Services;
using System.Security.Claims;

namespace EduConnect.Controllers
{
    [Authorize(Roles = "Faculty,Admin")]
    public class FacultyController : Controller
    {
        private static readonly string[] AllowedUploadExtensions =
        {
            ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".txt", ".md",
            ".zip", ".png", ".jpg", ".jpeg", ".gif", ".mp4", ".webm", ".mov",
        };

        private readonly ApplicationDbContext _context;
        private readonly IAIService _aiService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FacultyController> _logger;

        public FacultyController(ApplicationDbContext context, IAIService aiService, IWebHostEnvironment env, ILogger<FacultyController> logger)
        {
            _context = context;
            _aiService = aiService;
            _env = env;
            _logger = logger;
        }

        private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // ==================== DASHBOARD ====================

        /// <summary>Shortcut so /Faculty lands on the dashboard.</summary>
        public IActionResult Index() => RedirectToAction(nameof(Dashboard));

        public async Task<IActionResult> Dashboard()
        {
            var userId = CurrentUserId;

            var courses = await _context.Courses.AsNoTracking()
                .Where(c => c.FacultyId == userId)
                .Select(c => new FacultyCourseItem
                {
                    Course = c,
                    EnrollmentCount = c.Enrollments!.Count,
                    TopicCount = c.Topics!.Count,
                    MaterialCount = c.Materials!.Count,
                    QuizCount = c.Quizzes!.Count,
                    AverageProgress = c.Enrollments!.Any() ? Math.Round(c.Enrollments!.Average(e => (double)e.ProgressPercentage), 1) : 0,
                })
                .ToListAsync();

            var model = new FacultyDashboardViewModel
            {
                TotalCourses = courses.Count,
                TotalStudents = courses.Sum(c => c.EnrollmentCount),
                TotalQuizzes = courses.Sum(c => c.QuizCount),
                TotalAnnouncements = await _context.Announcements.CountAsync(a => a.FacultyId == userId),
                Courses = courses.OrderByDescending(c => c.Course.CreatedAt).ToList(),
                EnrollmentsPerCourse = courses
                    .OrderByDescending(c => c.EnrollmentCount)
                    .Select(c => new ChartPoint(c.Course.Title ?? "Untitled", c.EnrollmentCount))
                    .ToList(),
            };

            model.RecentQuizAttempts = await _context.QuizResults.AsNoTracking()
                .Where(r => r.Quiz!.Course!.FacultyId == userId)
                .Include(r => r.Student)
                .Include(r => r.Quiz)
                .OrderByDescending(r => r.AttemptedAt)
                .Take(8)
                .Select(r => new RecentActivityItem
                {
                    StudentName = r.Student!.FullName ?? r.Student.Email,
                    Target = r.Quiz!.Title,
                    Detail = r.PercentageScore.ToString("0") + "%",
                    OccurredAt = r.AttemptedAt,
                    Success = r.IsPassed,
                })
                .ToListAsync();

            return View(model);
        }

        // ==================== COURSES ====================

        [HttpGet]
        public IActionResult CreateCourse() => View(new CourseFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(CourseFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var course = new Course
            {
                Title = model.Title,
                Description = model.Description,
                Category = model.Category,
                IsActive = model.IsActive,
                FacultyId = CurrentUserId,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Add(course);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Course '{course.Title}' created. Now add topics, materials and quizzes.";
            return RedirectToAction(nameof(CourseDetails), new { id = course.Id });
        }

        [HttpGet]
        public async Task<IActionResult> EditCourse(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();
            if (course.FacultyId != CurrentUserId) return Forbid();

            return View(new CourseFormViewModel
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                Category = course.Category,
                IsActive = course.IsActive,
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(int id, CourseFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();
            if (course.FacultyId != CurrentUserId) return Forbid();

            if (!ModelState.IsValid) return View(model);

            course.Title = model.Title;
            course.Description = model.Description;
            course.Category = model.Category;
            course.IsActive = model.IsActive;
            course.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Course updated successfully.";
            return RedirectToAction(nameof(CourseDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null || course.FacultyId != CurrentUserId) return Forbid();

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Course '{course.Title}' and all of its content were deleted.";
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> CourseDetails(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Topics)
                .Include(c => c.Materials)
                .Include(c => c.Enrollments)
                .Include(c => c.Quizzes!)
                    .ThenInclude(q => q.Questions)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();
            if (course.FacultyId != CurrentUserId) return Forbid();

            return View(course);
        }

        public async Task<IActionResult> CourseStudents(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (course == null) return NotFound();
            if (course.FacultyId != CurrentUserId) return Forbid();

            var quizIds = await _context.Quizzes.Where(q => q.CourseId == id).Select(q => q.Id).ToListAsync();

            var quizStats = await _context.QuizResults.AsNoTracking()
                .Where(r => quizIds.Contains(r.QuizId))
                .GroupBy(r => r.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    Taken = g.Select(r => r.QuizId).Distinct().Count(),
                    Avg = g.Average(r => r.PercentageScore),
                    Last = g.Max(r => r.AttemptedAt),
                })
                .ToDictionaryAsync(x => x.StudentId!, x => x);

            var students = await _context.Enrollments.AsNoTracking()
                .Where(e => e.CourseId == id)
                .Include(e => e.Student)
                .OrderBy(e => e.Student!.FullName)
                .Select(e => new CourseStudentItem
                {
                    StudentId = e.StudentId!,
                    Name = e.Student!.FullName ?? e.Student.Email,
                    Email = e.Student.Email,
                    EnrolledAt = e.EnrolledAt,
                    ProgressPercentage = e.ProgressPercentage,
                    IsCompleted = e.IsCompleted,
                })
                .ToListAsync();

            foreach (var s in students)
            {
                if (quizStats.TryGetValue(s.StudentId, out var stat))
                {
                    s.QuizzesTaken = stat.Taken;
                    s.AverageQuizScore = Math.Round(stat.Avg, 1);
                    s.LastQuizAttempt = stat.Last;
                }
            }

            return View(new CourseStudentsViewModel { Course = course, Students = students });
        }

        // ==================== MATERIALS ====================

        [HttpGet]
        public async Task<IActionResult> UploadMaterial(int? courseId)
        {
            if (courseId == null) return NotFound();

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();
            if (course.FacultyId != CurrentUserId) return Forbid();

            ViewBag.CourseTitle = course.Title;
            return View(new MaterialUploadViewModel { CourseId = courseId.Value });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(209_715_200)] // 200 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 209_715_200)]
        public async Task<IActionResult> UploadMaterial(MaterialUploadViewModel model, IFormFile? file)
        {
            var course = await _context.Courses.FindAsync(model.CourseId);
            if (course == null) return NotFound();
            if (course.FacultyId != CurrentUserId) return Forbid();

            ViewBag.CourseTitle = course.Title;

            var isTextMaterial = string.Equals(model.FileType, "Text", StringComparison.OrdinalIgnoreCase);
            if (!isTextMaterial && (file == null || file.Length == 0))
                ModelState.AddModelError("file", "Please select a file to upload.");
            if (isTextMaterial && string.IsNullOrWhiteSpace(model.Description))
                ModelState.AddModelError(nameof(model.Description), "Text materials need their content in the description field.");

            string? storedPath = null;
            long fileSize = 0;

            if (file != null && file.Length > 0)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedUploadExtensions.Contains(extension))
                    ModelState.AddModelError("file", $"File type '{extension}' is not allowed.");
            }

            if (!ModelState.IsValid) return View(model);

            try
            {
                if (file != null && file.Length > 0)
                {
                    var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", "materials");
                    Directory.CreateDirectory(uploadsPath);

                    var safeName = Path.GetFileName(file.FileName);
                    var fileName = $"{Guid.NewGuid()}_{safeName}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    await using (var stream = new FileStream(filePath, FileMode.Create))
                        await file.CopyToAsync(stream);

                    storedPath = $"/uploads/materials/{fileName}";
                    fileSize = file.Length;
                }

                _context.Add(new Material
                {
                    Title = model.Title,
                    Description = model.Description,
                    FileType = model.FileType,
                    FilePath = storedPath ?? string.Empty,
                    CourseId = model.CourseId,
                    UploadedAt = DateTime.UtcNow,
                    FileSize = fileSize,
                });
                await _context.SaveChangesAsync();

                TempData["Success"] = "Material added successfully.";
                return RedirectToAction(nameof(CourseDetails), new { id = model.CourseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading material for course {CourseId}", model.CourseId);
                ModelState.AddModelError("file", $"Error uploading file: {ex.Message}");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMaterial(int materialId, int courseId)
        {
            var material = await _context.Materials.FindAsync(materialId);
            if (material == null) return NotFound();

            var course = await _context.Courses.FindAsync(courseId);
            if (course?.FacultyId != CurrentUserId) return Forbid();

            try
            {
                if (!string.IsNullOrEmpty(material.FilePath))
                {
                    var filePath = Path.Combine(_env.WebRootPath, material.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not remove material file {Path}", material.FilePath);
            }

            _context.Materials.Remove(material);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Material deleted.";
            return RedirectToAction(nameof(CourseDetails), new { id = courseId });
        }

        // ==================== TOPICS ====================

        [HttpGet]
        public async Task<IActionResult> AddTopic(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();
            if (course.FacultyId != CurrentUserId) return Forbid();

            ViewBag.CourseName = course.Title;
            return View(new TopicFormViewModel { CourseId = courseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(52_428_800)] // 50 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 52_428_800)]
        public async Task<IActionResult> AddTopic(TopicFormViewModel model, IFormFile? pdfFile)
        {
            var course = await _context.Courses.FindAsync(model.CourseId);
            if (course == null) return NotFound();
            if (course.FacultyId != CurrentUserId) return Forbid();

            ViewBag.CourseName = course.Title;

            if (pdfFile is { Length: > 0 } && !string.Equals(Path.GetExtension(pdfFile.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError("pdfFile", "Only PDF files are allowed for topic documents.");

            if (!ModelState.IsValid) return View(model);

            var topic = new Topic
            {
                CourseId = model.CourseId,
                Name = model.Name!,
                Description = model.Description!,
                PdfFilePath = string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            if (pdfFile is { Length: > 0 })
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "topics");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid() + ".pdf";
                var filePath = Path.Combine(uploadsFolder, fileName);
                await using (var stream = new FileStream(filePath, FileMode.Create))
                    await pdfFile.CopyToAsync(stream);

                topic.PdfFilePath = "/uploads/topics/" + fileName;
            }

            _context.Topics.Add(topic);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Topic added successfully.";
            return RedirectToAction(nameof(CourseDetails), new { id = model.CourseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTopic(int topicId, int courseId)
        {
            var topic = await _context.Topics.FindAsync(topicId);
            if (topic == null) return NotFound();

            var course = await _context.Courses.FindAsync(courseId);
            if (course?.FacultyId != CurrentUserId) return Forbid();

            if (!string.IsNullOrEmpty(topic.PdfFilePath))
            {
                var filePath = Path.Combine(_env.WebRootPath, topic.PdfFilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            _context.Topics.Remove(topic);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Topic deleted.";
            return RedirectToAction(nameof(CourseDetails), new { id = courseId });
        }

        // ==================== QUIZZES ====================

        [HttpGet]
        public async Task<IActionResult> CreateQuiz(int? courseId)
        {
            if (courseId == null) return NotFound();

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();
            if (course.FacultyId != CurrentUserId) return Forbid();

            ViewBag.CourseTitle = course.Title;
            return View(new QuizFormViewModel { CourseId = courseId.Value });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuiz(QuizFormViewModel model)
        {
            var course = await _context.Courses.FindAsync(model.CourseId);
            if (course == null) return NotFound();
            if (course.FacultyId != CurrentUserId) return Forbid();

            ViewBag.CourseTitle = course.Title;
            if (!ModelState.IsValid) return View(model);

            var quiz = new Quiz
            {
                CourseId = model.CourseId,
                Title = model.Title,
                Description = model.Description,
                PassingMarks = model.PassingMarks,
                DurationInMinutes = model.DurationInMinutes,
                TotalQuestions = 0,
                TotalMarks = 0,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };
            _context.Add(quiz);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Quiz created — now add questions.";
            return RedirectToAction(nameof(AddQuizQuestion), new { quizId = quiz.Id });
        }

        public async Task<IActionResult> QuizDetails(int? id)
        {
            if (id == null) return NotFound();

            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return NotFound();
            if (quiz.Course?.FacultyId != CurrentUserId) return Forbid();

            var results = await _context.QuizResults.AsNoTracking()
                .Where(r => r.QuizId == id)
                .Include(r => r.Student)
                .OrderByDescending(r => r.AttemptedAt)
                .ToListAsync();

            var model = new QuizDetailsViewModel
            {
                Quiz = quiz,
                AttemptCount = results.Count,
                AverageScore = results.Count > 0 ? Math.Round(results.Average(r => r.PercentageScore), 1) : 0,
                PassRate = results.Count > 0 ? Math.Round(results.Count(r => r.IsPassed) * 100.0 / results.Count, 1) : 0,
                RecentResults = results.Take(15).ToList(),
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleQuizActive(int quizId)
        {
            var quiz = await _context.Quizzes.Include(q => q.Course).FirstOrDefaultAsync(q => q.Id == quizId);
            if (quiz == null) return NotFound();
            if (quiz.Course?.FacultyId != CurrentUserId) return Forbid();

            quiz.IsActive = !quiz.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = quiz.IsActive ? "Quiz is now visible to students." : "Quiz hidden from students.";
            return RedirectToAction(nameof(QuizDetails), new { id = quizId });
        }

        [HttpGet]
        public async Task<IActionResult> AddQuizQuestion(int? quizId)
        {
            if (quizId == null) return NotFound();

            var quiz = await _context.Quizzes.Include(q => q.Course).Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == quizId);
            if (quiz == null) return NotFound();
            if (quiz.Course?.FacultyId != CurrentUserId) return Forbid();

            ViewBag.Quiz = quiz;
            return View(new QuestionFormViewModel { QuizId = quizId.Value });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuizQuestion(QuestionFormViewModel model, string? addAnother)
        {
            var quiz = await _context.Quizzes.Include(q => q.Course).Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == model.QuizId);
            if (quiz == null) return NotFound();
            if (quiz.Course?.FacultyId != CurrentUserId) return Forbid();

            ViewBag.Quiz = quiz;

            var type = model.QuestionType?.Trim().ToLowerInvariant() switch
            {
                "coding" => QuestionType.Coding,
                "truefalse" => QuestionType.TrueFalse,
                _ => QuestionType.MultipleChoice,
            };

            // Type-specific validation
            if (type == QuestionType.Coding)
            {
                if (string.IsNullOrWhiteSpace(model.CodeTemplate))
                    ModelState.AddModelError(nameof(model.CodeTemplate), "A code template is required for coding questions.");
                if (string.IsNullOrWhiteSpace(model.ExpectedOutput))
                    ModelState.AddModelError(nameof(model.ExpectedOutput), "Expected output is required for coding questions.");
            }
            else if (type == QuestionType.TrueFalse)
            {
                if (model.CorrectOption != "True" && model.CorrectOption != "False")
                    ModelState.AddModelError(nameof(model.CorrectOption), "Choose True or False as the correct answer.");
            }
            else
            {
                foreach (var (value, name) in new[] { (model.OptionA, nameof(model.OptionA)), (model.OptionB, nameof(model.OptionB)), (model.OptionC, nameof(model.OptionC)), (model.OptionD, nameof(model.OptionD)) })
                {
                    if (string.IsNullOrWhiteSpace(value))
                        ModelState.AddModelError(name, "All four options are required.");
                }
                if (string.IsNullOrWhiteSpace(model.CorrectOption) || !new[] { "A", "B", "C", "D" }.Contains(model.CorrectOption))
                    ModelState.AddModelError(nameof(model.CorrectOption), "Choose the correct option (A–D).");
            }

            if (!ModelState.IsValid) return View(model);

            var question = new QuizQuestion
            {
                QuizId = model.QuizId,
                QuestionText = model.QuestionText,
                QuestionType = type switch
                {
                    QuestionType.Coding => "Coding",
                    QuestionType.TrueFalse => "TrueFalse",
                    _ => "MCQ",
                },
                QuestionTypeEnum = type,
                OptionA = type == QuestionType.TrueFalse ? "True" : model.OptionA,
                OptionB = type == QuestionType.TrueFalse ? "False" : model.OptionB,
                OptionC = type == QuestionType.MultipleChoice ? model.OptionC : null,
                OptionD = type == QuestionType.MultipleChoice ? model.OptionD : null,
                CorrectOption = model.CorrectOption ?? (type == QuestionType.TrueFalse ? "True" : "A"),
                Marks = model.Marks,
                Difficulty = model.Difficulty ?? "Medium",
                Order = (quiz.Questions?.Count ?? 0) + 1,
                CodeTemplate = type == QuestionType.Coding ? model.CodeTemplate : null,
                ExpectedOutput = type == QuestionType.Coding ? model.ExpectedOutput : null,
                ProgrammingLanguage = type == QuestionType.Coding ? (model.ProgrammingLanguage ?? "python") : null,
            };

            _context.Add(question);
            quiz.TotalQuestions += 1;
            quiz.TotalMarks += model.Marks;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Question added.";
            return addAnother == "true"
                ? RedirectToAction(nameof(AddQuizQuestion), new { quizId = model.QuizId })
                : RedirectToAction(nameof(QuizDetails), new { id = model.QuizId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuiz(int quizId, int courseId)
        {
            var quiz = await _context.Quizzes.Include(q => q.Course).FirstOrDefaultAsync(q => q.Id == quizId);
            if (quiz == null) return NotFound();
            if (quiz.Course?.FacultyId != CurrentUserId) return Forbid();

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Quiz deleted.";
            return RedirectToAction(nameof(CourseDetails), new { id = courseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int questionId, int? courseId)
        {
            var question = await _context.QuizQuestions
                .Include(q => q.Quiz!)
                    .ThenInclude(quiz => quiz.Course)
                .FirstOrDefaultAsync(q => q.Id == questionId);
            if (question == null) return NotFound();
            if (question.Quiz?.Course?.FacultyId != CurrentUserId) return Forbid();

            var quiz = question.Quiz!;
            quiz.TotalQuestions = Math.Max(0, quiz.TotalQuestions - 1);
            quiz.TotalMarks = Math.Max(0, quiz.TotalMarks - question.Marks);

            _context.QuizQuestions.Remove(question);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Question deleted.";
            return RedirectToAction(nameof(QuizDetails), new { id = quiz.Id });
        }

        // ==================== ANNOUNCEMENTS ====================

        public async Task<IActionResult> Announcements()
        {
            var userId = CurrentUserId;
            var announcements = await _context.Announcements.AsNoTracking()
                .Where(a => a.FacultyId == userId)
                .Include(a => a.Course)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return View(announcements);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAnnouncement()
        {
            await LoadCourseOptions();
            return View(new AnnouncementFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncement(AnnouncementFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadCourseOptions();
                return View(model);
            }

            if (model.CourseId.HasValue)
            {
                var course = await _context.Courses.FindAsync(model.CourseId.Value);
                if (course == null || course.FacultyId != CurrentUserId)
                {
                    ModelState.AddModelError(nameof(model.CourseId), "Please pick one of your own courses.");
                    await LoadCourseOptions();
                    return View(model);
                }
            }

            _context.Announcements.Add(new Announcement
            {
                Title = model.Title,
                Content = model.Content,
                CourseId = model.CourseId,
                FacultyId = CurrentUserId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Announcement published.";
            return RedirectToAction(nameof(Announcements));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAnnouncement(int id)
        {
            var announcement = await _context.Announcements.FirstOrDefaultAsync(a => a.Id == id && a.FacultyId == CurrentUserId);
            if (announcement == null) return NotFound();

            announcement.IsActive = !announcement.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = announcement.IsActive ? "Announcement is now visible." : "Announcement hidden.";
            return RedirectToAction(nameof(Announcements));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            var announcement = await _context.Announcements.FirstOrDefaultAsync(a => a.Id == id && a.FacultyId == CurrentUserId);
            if (announcement == null) return NotFound();

            _context.Announcements.Remove(announcement);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Announcement deleted.";
            return RedirectToAction(nameof(Announcements));
        }

        // ==================== AI ASSISTANCE ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateTopicsWithAI(int courseId)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null) return NotFound();
            if (course.FacultyId != CurrentUserId) return Forbid();

            try
            {
                var topics = await _aiService.GenerateTopicsAsync(course.Title ?? "", course.Description ?? "");
                if (topics.Count == 0)
                {
                    TempData["Error"] = "Could not generate topics. Please try again.";
                    return RedirectToAction(nameof(CourseDetails), new { id = courseId });
                }

                foreach (var topicName in topics)
                {
                    _context.Topics.Add(new Topic
                    {
                        CourseId = courseId,
                        Name = topicName,
                        Description = $"Auto-generated topic: {topicName}",
                        PdfFilePath = string.Empty,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    });
                }
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Generated {topics.Count} topics with AI.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating topics for course {CourseId}", courseId);
                TempData["Error"] = $"Error generating topics: {ex.Message}";
            }
            return RedirectToAction(nameof(CourseDetails), new { id = courseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateMaterialWithAI(int topicId)
        {
            var topic = await _context.Topics.Include(t => t.Course).FirstOrDefaultAsync(t => t.Id == topicId);
            if (topic?.Course == null) return NotFound();
            if (topic.Course.FacultyId != CurrentUserId) return Forbid();

            try
            {
                var content = await _aiService.GenerateMaterialContentAsync(topic.Course.Title ?? "", topic.Name);
                if (string.IsNullOrEmpty(content))
                {
                    TempData["Error"] = "Could not generate material content. Please try again.";
                    return RedirectToAction(nameof(CourseDetails), new { id = topic.CourseId });
                }

                _context.Materials.Add(new Material
                {
                    TopicId = topicId,
                    CourseId = topic.CourseId,
                    Title = $"{topic.Name} — Learning Material",
                    Description = content,
                    FileType = "Text",
                    FilePath = string.Empty,
                    UploadedAt = DateTime.UtcNow,
                });
                await _context.SaveChangesAsync();

                TempData["Success"] = "Material generated and added.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating material for topic {TopicId}", topicId);
                TempData["Error"] = $"Error generating material: {ex.Message}";
            }
            return RedirectToAction(nameof(CourseDetails), new { id = topic.CourseId });
        }

        /// <summary>Generates questions for an existing quiz using AI, by requested counts per type.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateQuestionsFromPrompt([FromBody] GenerateQuestionsRequest model)
        {
            if (model == null || model.QuizId <= 0) return Json(new { success = false, message = "QuizId is required" });

            var quiz = await _context.Quizzes.Include(q => q.Course).Include(q => q.Topic).FirstOrDefaultAsync(q => q.Id == model.QuizId);
            if (quiz == null) return Json(new { success = false, message = "Quiz not found" });
            if (quiz.Course?.FacultyId != CurrentUserId) return Json(new { success = false, message = "Unauthorized" });

            var allQuestions = new List<QuizQuestionData>();
            try
            {
                var subject = quiz.Topic?.Name ?? quiz.Title ?? "";
                if (model.MCCount > 0)
                    allQuestions.AddRange(await _aiService.GenerateMultipleChoiceQuestionsAsync(quiz.Course.Title ?? "", subject, model.MCCount));
                if (model.TFCount > 0)
                    allQuestions.AddRange(await _aiService.GenerateTrueFalseQuestionsAsync(quiz.Course.Title ?? "", subject, model.TFCount));
                if (model.CodingCount > 0)
                    allQuestions.AddRange(await _aiService.GenerateCodingQuestionsAsync(quiz.Course.Title ?? "", subject, model.CodingCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI generation failed for quiz {QuizId}", model.QuizId);
                return Json(new { success = false, message = "AI generation failed" });
            }

            if (allQuestions.Count == 0) return Json(new { success = false, message = "No questions generated" });

            var nextOrder = await _context.QuizQuestions.Where(q => q.QuizId == quiz.Id).MaxAsync(q => (int?)q.Order) ?? 0;
            foreach (var qd in allQuestions)
            {
                nextOrder++;
                _context.QuizQuestions.Add(new QuizQuestion
                {
                    QuizId = quiz.Id,
                    QuestionText = qd.Question,
                    OptionA = qd.OptionA,
                    OptionB = qd.OptionB,
                    OptionC = qd.OptionC,
                    OptionD = qd.OptionD,
                    CorrectOption = qd.CorrectOption,
                    Marks = qd.Marks,
                    QuestionType = qd.QuestionType,
                    Difficulty = qd.Difficulty,
                    Order = nextOrder,
                });
            }

            quiz.TotalQuestions += allQuestions.Count;
            quiz.TotalMarks += allQuestions.Sum(q => q.Marks);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Added {allQuestions.Count} questions to the quiz." });
        }

        /// <summary>Serves a generated topic PDF. Access limited to signed-in users; path traversal blocked.</summary>
        [HttpGet]
        public IActionResult DownloadTopicPdf(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return NotFound();

            var safeName = Path.GetFileName(fileName);
            var filePath = Path.Combine(_env.WebRootPath, "uploads", "topics", safeName);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            return PhysicalFile(filePath, "application/pdf", safeName);
        }

        // ==================== ATTENDANCE ====================

        [HttpGet]
        public async Task<IActionResult> ManageAttendance(DateTime? date)
        {
            var attendanceDate = (date ?? DateTime.UtcNow).Date;

            var students = await (
                from user in _context.Users
                join userRole in _context.UserRoles on user.Id equals userRole.UserId
                join role in _context.Roles on userRole.RoleId equals role.Id
                where role.Name == "Student" && user.IsActive
                orderby user.FullName
                select user).AsNoTracking().ToListAsync();

            var dayAttendance = await _context.Attendances.AsNoTracking()
                .Where(a => a.AttendanceDate.Date == attendanceDate)
                .ToListAsync();

            ViewBag.AttendanceDate = attendanceDate;
            ViewBag.TodayAttendance = dayAttendance
                .GroupBy(a => a.StudentId)
                .ToDictionary(g => g.Key, g => g.First().Status);
            ViewBag.TodayRemarks = dayAttendance
                .GroupBy(a => a.StudentId)
                .ToDictionary(g => g.Key, g => g.First().Remarks);
            return View(students);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageAttendance(IFormCollection form)
        {
            try
            {
                var attendanceDate = DateTime.TryParse(form["attendanceDate"], out var parsed)
                    ? parsed.Date
                    : DateTime.UtcNow.Date;

                var studentIds = form["studentIds"].ToList();
                var existing = await _context.Attendances
                    .Where(a => a.AttendanceDate.Date == attendanceDate)
                    .ToDictionaryAsync(a => a.StudentId!, a => a);

                foreach (var studentId in studentIds)
                {
                    if (string.IsNullOrEmpty(studentId)) continue;

                    var status = form[$"status_{studentId}"].ToString();
                    var remarks = form[$"remarks_{studentId}"].ToString();
                    if (string.IsNullOrEmpty(status)) continue;

                    if (existing.TryGetValue(studentId, out var record))
                    {
                        record.Status = status;
                        record.Remarks = remarks;
                    }
                    else
                    {
                        _context.Attendances.Add(new Attendance
                        {
                            StudentId = studentId,
                            CourseId = null,
                            AttendanceDate = attendanceDate,
                            Status = status,
                            Remarks = remarks,
                            CreatedAt = DateTime.UtcNow,
                        });
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Attendance for {attendanceDate:dd MMM yyyy} saved.";
                return RedirectToAction(nameof(ManageAttendance), new { date = attendanceDate.ToString("yyyy-MM-dd") });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving attendance");
                TempData["Error"] = "Error saving attendance: " + ex.Message;
                return RedirectToAction(nameof(ManageAttendance));
            }
        }

        [HttpGet]
        public async Task<IActionResult> AttendanceList(DateTime? from, DateTime? to)
        {
            var fromDate = (from ?? DateTime.UtcNow.AddDays(-30)).Date;
            var toDate = (to ?? DateTime.UtcNow).Date;

            var records = await _context.Attendances.AsNoTracking()
                .Include(a => a.Student)
                .Where(a => a.AttendanceDate.Date >= fromDate && a.AttendanceDate.Date <= toDate)
                .OrderByDescending(a => a.AttendanceDate)
                .ThenBy(a => a.Student!.FullName)
                .ToListAsync();

            ViewBag.From = fromDate;
            ViewBag.To = toDate;
            return View(records);
        }

        private async Task LoadCourseOptions()
        {
            var userId = CurrentUserId;
            ViewBag.Courses = await _context.Courses.AsNoTracking()
                .Where(c => c.FacultyId == userId)
                .OrderBy(c => c.Title)
                .ToListAsync();
        }
    }
}
