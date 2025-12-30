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
        private readonly ApplicationDbContext _context;
        private readonly IAIService _aiService;
        private readonly PdfGenerationService _pdfService;
        private readonly ILogger<FacultyController> _logger;

        public FacultyController(ApplicationDbContext context, IAIService aiService, PdfGenerationService pdfService, ILogger<FacultyController> logger)
        {
            _context = context;
            _aiService = aiService;
            _pdfService = pdfService;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var courses = await _context.Courses
                .Where(c => c.FacultyId == userId)
                .Include(c => c.Enrollments)
                .ToListAsync();

            var totalStudents = await _context.Enrollments
                .Where(e => e.Course != null && e.Course.FacultyId == userId)
                .CountAsync();

            var announcements = await _context.Announcements
                .Where(a => a.FacultyId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            ViewBag.TotalCourses = courses.Count;
            ViewBag.TotalStudents = totalStudents;
            ViewBag.TotalAnnouncements = announcements.Count;

            return View(courses);
        }

        [HttpGet]
        public IActionResult CreateCourse()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse(Course course)
        {
            if (ModelState.IsValid)
            {
                course.FacultyId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                course.CreatedAt = DateTime.UtcNow;
                _context.Add(course);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Dashboard));
            }
            return View(course);
        }

        [HttpGet]
        public async Task<IActionResult> EditCourse(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (course.FacultyId != userId) return Forbid();

            return View(course);
        }

        [HttpPost]
        public async Task<IActionResult> EditCourse(int id, Course course)
        {
            if (id != course.Id) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var existingCourse = await _context.Courses.FindAsync(id);

            if (existingCourse == null || existingCourse.FacultyId != userId) return Forbid();

            if (ModelState.IsValid)
            {
                existingCourse.Title = course.Title;
                existingCourse.Description = course.Description;
                existingCourse.Category = course.Category;
                existingCourse.UpdatedAt = DateTime.UtcNow;
                existingCourse.IsActive = course.IsActive;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Dashboard));
            }
            return View(course);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (course == null || course.FacultyId != userId) return Forbid();

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> CourseDetails(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Topics)
                .Include(c => c.Materials)
                .Include(c => c.Enrollments)
                .Include(c => c.Quizzes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (course.FacultyId != userId) return Forbid();

            return View(course);
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> UploadMaterial(int? courseId)
        {
            if (courseId == null) return NotFound();

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (course.FacultyId != userId) return Forbid();

            var material = new Material { CourseId = courseId.Value };
            return View(material);
        }

        [HttpPost]
        public async Task<IActionResult> UploadMaterial(int courseId, string title, string description, string fileType, IFormFile file)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (course.FacultyId != userId) return Forbid();

            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Please select a file to upload.");
                ViewBag.CourseId = courseId;
                return View();
            }

            try
            {
                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "materials");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create Material record
                var material = new Material
                {
                    Title = title,
                    Description = description,
                    FileType = fileType,
                    FilePath = $"/uploads/materials/{fileName}",
                    CourseId = courseId,
                    UploadedAt = DateTime.UtcNow,
                    FileSize = file.Length
                };

                _context.Add(material);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Material uploaded successfully!";
                return RedirectToAction(nameof(CourseDetails), new { id = courseId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("file", $"Error uploading file: {ex.Message}");
                ViewBag.CourseId = courseId;
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMaterial(int materialId, int courseId)
        {
            var material = await _context.Materials.FindAsync(materialId);
            if (material == null) return NotFound();

            var course = await _context.Courses.FindAsync(courseId);
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (course?.FacultyId != userId) return Forbid();

            try
            {
                // Delete file from disk
                if (!string.IsNullOrEmpty(material.FilePath))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", material.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }
            catch { }

            _context.Materials.Remove(material);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Material deleted successfully!";
            return RedirectToAction(nameof(CourseDetails), new { id = courseId });
        }

        [HttpGet]
        public async Task<IActionResult> CreateQuiz(int? courseId)
        {
            if (courseId == null) return NotFound();

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (course.FacultyId != userId) return Forbid();

            ViewBag.CourseId = courseId;
            ViewBag.CourseTitle = course.Title;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateQuiz(int courseId, string title, string description, int passingMarks, int durationInMinutes)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (course.FacultyId != userId) return Forbid();

            if (string.IsNullOrEmpty(title) || passingMarks <= 0 || durationInMinutes <= 0)
            {
                ModelState.AddModelError("", "Please fill in all fields properly.");
                ViewBag.CourseId = courseId;
                ViewBag.CourseTitle = course.Title;
                return View();
            }

            var quiz = new Quiz
            {
                CourseId = courseId,
                Title = title,
                Description = description,
                PassingMarks = passingMarks,
                DurationInMinutes = durationInMinutes,
                TotalQuestions = 0,
                TotalMarks = 0
            };

            _context.Add(quiz);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Quiz created successfully! Now add questions to it.";
            return RedirectToAction(nameof(AddQuizQuestion), new { quizId = quiz.Id });
        }

        [HttpGet]
        public async Task<IActionResult> AddQuizQuestion(int? quizId)
        {
            if (quizId == null) return NotFound();

            var quiz = await _context.Quizzes.Include(q => q.Course).FirstOrDefaultAsync(q => q.Id == quizId);
            if (quiz == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (quiz.Course?.FacultyId != userId) return Forbid();

            ViewBag.QuizId = quizId;
            ViewBag.QuizTitle = quiz.Title;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddQuizQuestion(int quizId, QuizQuestion model)
        {
            var quiz = await _context.Quizzes.Include(q => q.Course).FirstOrDefaultAsync(q => q.Id == quizId);
            if (quiz == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (quiz.Course?.FacultyId != userId) return Forbid();

            // Validate based on question type
            if (model.QuestionType == QuestionType.Coding)
            {
                // Coding question validation
                if (string.IsNullOrEmpty(model.QuestionText) || string.IsNullOrEmpty(model.CodeTemplate) || 
                    string.IsNullOrEmpty(model.ExpectedOutput) || model.Marks <= 0)
                {
                    ModelState.AddModelError("", "Please fill in all coding question fields.");
                    ViewBag.QuizId = quizId;
                    ViewBag.QuizTitle = quiz.Title;
                    return View();
                }
            }
            else
            {
                // Multiple choice or true/false validation
                if (string.IsNullOrEmpty(model.QuestionText) || string.IsNullOrEmpty(model.OptionA) || 
                    string.IsNullOrEmpty(model.OptionB) || string.IsNullOrEmpty(model.OptionC) || 
                    string.IsNullOrEmpty(model.OptionD) || model.Marks <= 0)
                {
                    ModelState.AddModelError("", "Please fill in all fields properly.");
                    ViewBag.QuizId = quizId;
                    ViewBag.QuizTitle = quiz.Title;
                    return View();
                }
            }

            var question = new QuizQuestion
            {
                QuizId = quizId,
                QuestionText = model.QuestionText,
                QuestionType = model.QuestionType,
                OptionA = model.OptionA,
                OptionB = model.OptionB,
                OptionC = model.OptionC,
                OptionD = model.OptionD,
                CorrectOption = char.ToUpper(model.CorrectOption),
                Marks = model.Marks,
                CodeTemplate = model.CodeTemplate,
                ExpectedOutput = model.ExpectedOutput,
                ProgrammingLanguage = model.ProgrammingLanguage ?? "csharp"
            };

            _context.Add(question);

            // Update quiz TotalQuestions and TotalMarks
            quiz.TotalQuestions += 1;
            quiz.TotalMarks += model.Marks;
            _context.Update(quiz);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Question added successfully!";
            return RedirectToAction(nameof(CourseDetails), new { id = quiz.CourseId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteQuiz(int quizId, int courseId)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == quizId);
            
            if (quiz == null) return NotFound();

            var course = await _context.Courses.FindAsync(courseId);
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (course?.FacultyId != userId) return Forbid();

            try
            {
                // Delete the quiz - cascade will handle QuizResults and QuizQuestions
                _context.Quizzes.Remove(quiz);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Quiz deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting quiz: {ex.Message}";
            }

            return RedirectToAction(nameof(CourseDetails), new { id = courseId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteQuestion(int questionId, int courseId)
        {
            var question = await _context.QuizQuestions.Include(q => q.Quiz).FirstOrDefaultAsync(q => q.Id == questionId);
            if (question == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (question.Quiz?.Course?.FacultyId != userId) return Forbid();

            var quiz = question.Quiz;
            if (quiz != null)
            {
                quiz.TotalQuestions -= 1;
                quiz.TotalMarks -= question.Marks;
                _context.Update(quiz);
            }

            _context.QuizQuestions.Remove(question);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Question deleted successfully!";
            return RedirectToAction(nameof(CourseDetails), new { id = courseId });
        }

        // Topic Management
        [HttpGet]
        public async Task<IActionResult> AddTopic(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (course.FacultyId != userId) return Forbid();

            ViewBag.CourseId = courseId;
            ViewBag.CourseName = course.Title;
            return View(new Topic { CourseId = courseId });
        }

        [HttpPost]
        public async Task<IActionResult> AddTopic(int courseId, string name, string description, IFormFile pdfFile)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (course.FacultyId != userId) return Forbid();

            var topic = new Topic
            {
                CourseId = courseId,
                Name = name,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            // Handle PDF upload
            if (pdfFile != null && pdfFile.Length > 0)
            {
                try
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "topics");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(pdfFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await pdfFile.CopyToAsync(stream);
                    }

                    topic.PdfFilePath = "/uploads/topics/" + fileName;
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error uploading PDF: " + ex.Message;
                    return RedirectToAction(nameof(CourseDetails), new { id = courseId });
                }
            }

            _context.Topics.Add(topic);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Topic added successfully!";
            return RedirectToAction(nameof(CourseDetails), new { id = courseId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTopic(int topicId, int courseId)
        {
            var topic = await _context.Topics.FindAsync(topicId);
            if (topic == null) return NotFound();

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (course.FacultyId != userId) return Forbid();

            // Delete PDF file if it exists
            if (!string.IsNullOrEmpty(topic.PdfFilePath))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", topic.PdfFilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Topics.Remove(topic);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Topic deleted successfully!";
            return RedirectToAction(nameof(CourseDetails), new { id = courseId });
        }

        /// <summary>
        /// Generate topics using AI for a course
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GenerateTopicsWithAI(int courseId)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (course.FacultyId != userId) return Forbid();

            try
            {
                // Get AI Service from DI
                var aiService = HttpContext.RequestServices.GetRequiredService<IAIService>();
                
                // Generate topics
                var topics = await aiService.GenerateTopicsAsync(course.Title, course.Description ?? "");
                
                if (topics.Count == 0)
                {
                    TempData["Error"] = "Could not generate topics. Please try again.";
                    return RedirectToAction(nameof(CourseDetails), new { id = courseId });
                }

                // Create Topic entities
                foreach (var topicName in topics)
                {
                    var topic = new Topic
                    {
                        CourseId = courseId,
                        Name = topicName,
                        Description = $"Auto-generated topic: {topicName}",
                        PdfFilePath = "",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Topics.Add(topic);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Successfully generated {topics.Count} topics with AI!";
                return RedirectToAction(nameof(CourseDetails), new { id = courseId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error generating topics: {ex.Message}";
                return RedirectToAction(nameof(CourseDetails), new { id = courseId });
            }
        }

        /// <summary>
        /// Generate material content using AI for a topic
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GenerateMaterialWithAI(int topicId)
        {
            var topic = await _context.Topics.Include(t => t.Course).FirstOrDefaultAsync(t => t.Id == topicId);
            if (topic?.Course == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (topic.Course.FacultyId != userId) return Forbid();

            try
            {
                var aiService = HttpContext.RequestServices.GetRequiredService<IAIService>();
                
                // Generate material content
                var content = await aiService.GenerateMaterialContentAsync(topic.Course.Title, topic.Name);
                
                if (string.IsNullOrEmpty(content))
                {
                    TempData["Error"] = "Could not generate material content. Please try again.";
                    return RedirectToAction(nameof(CourseDetails), new { id = topic.CourseId });
                }

                // Create Material entity
                var material = new Material
                {
                    TopicId = topicId,
                    CourseId = topic.CourseId,
                    Title = $"{topic.Name} - Learning Material",
                    Description = content,
                    FileType = "Text",
                    FilePath = "",
                    UploadedAt = DateTime.UtcNow
                };

                _context.Materials.Add(material);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Material generated and added successfully!";
                return RedirectToAction(nameof(CourseDetails), new { id = topic.CourseId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error generating material: {ex.Message}";
                return RedirectToAction(nameof(CourseDetails), new { id = topic.CourseId });
            }
        }

        /// <summary>
        /// Generate quiz questions using AI for a topic
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GenerateQuizWithAI(int topicId)
        {
            var topic = await _context.Topics.Include(t => t.Course).FirstOrDefaultAsync(t => t.Id == topicId);
            if (topic?.Course == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (topic.Course.FacultyId != userId) return Forbid();

            try
            {
                var aiService = HttpContext.RequestServices.GetRequiredService<IAIService>();
                
                // Generate quiz questions
                var questions = await aiService.GenerateQuizQuestionsAsync(topic.Course.Title, topic.Name, 5);
                
                if (questions.Count == 0)
                {
                    TempData["Error"] = "Could not generate quiz questions. Please try again.";
                    return RedirectToAction(nameof(CourseDetails), new { id = topic.CourseId });
                }

                // Create Quiz entity
                var quiz = new Quiz
                {
                    CourseId = topic.CourseId,
                    TopicId = topicId,
                    Title = $"{topic.Name} - Quiz",
                    Description = "Auto-generated quiz using AI",
                    TotalMarks = questions.Count,
                    PassingMarks = (int)(questions.Count * 0.6), // 60% passing
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();

                // Add questions to the quiz
                foreach (var q in questions)
                {
                    var question = new QuizQuestion
                    {
                        QuizId = quiz.Id,
                        QuestionText = q.Question,
                        OptionA = q.OptionA,
                        OptionB = q.OptionB,
                        OptionC = q.OptionC,
                        OptionD = q.OptionD,
                        CorrectOption = char.Parse(q.CorrectOption),
                        Marks = q.Marks,
                        QuestionType = QuestionType.MultipleChoice
                    };
                    _context.QuizQuestions.Add(question);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Successfully generated quiz with {questions.Count} questions!";
                return RedirectToAction(nameof(CourseDetails), new { id = topic.CourseId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error generating quiz: {ex.Message}";
                return RedirectToAction(nameof(CourseDetails), new { id = topic.CourseId });
            }
        }

        /// <summary>
        /// Generate topics using AI from the create course form
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GenerateTopicsAI([FromBody] GenerateTopicsRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.CourseTitle) || string.IsNullOrEmpty(request.CourseDescription))
                {
                    return Json(new { success = false, message = "Course title and description are required" });
                }

                _logger.LogInformation("Generating topics for course: {CourseTitle}", request.CourseTitle);

                // Generate topics using AI
                var topics = await _aiService.GenerateTopicsAsync(request.CourseTitle, request.CourseDescription);

                if (topics == null || topics.Count == 0)
                {
                    _logger.LogWarning("Failed to generate topics for course: {CourseTitle}", request.CourseTitle);
                    return Json(new { success = false, message = "Failed to generate topics. Please try again. Check server logs for details." });
                }

                _logger.LogInformation("Successfully generated {TopicCount} topics for course: {CourseTitle}", topics.Count, request.CourseTitle);
                return Json(new { success = true, topics = topics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating topics");
                return Json(new { success = false, message = $"Error: {ex.GetBaseException().Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveGeneratedTopics([FromBody] SaveGeneratedTopicsRequest request)
        {
            try
            {
                if (request == null || request.CourseId <= 0 || request.Topics == null || request.Topics.Count == 0)
                {
                    return Json(new { success = false, message = "Course ID and topics are required" });
                }

                var course = await _context.Courses.FindAsync(request.CourseId);
                if (course == null)
                {
                    return Json(new { success = false, message = "Course not found" });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (course.FacultyId != userId)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                // Initialize upload path
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                // Create Topic entities and generate PDFs
                var topicEntities = new List<Topic>();
                foreach (var topicName in request.Topics)
                {
                    // Generate PDF for the topic
                    var pdfPath = await _pdfService.GenerateTopicPdfAsync(course.Title, topicName, uploadPath);

                    var topic = new Topic
                    {
                        Name = topicName,
                        Description = $"AI-generated topic for {course.Title}",
                        PdfFilePath = pdfPath ?? string.Empty, // store empty string when PDF generation failed
                        CourseId = request.CourseId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    topicEntities.Add(topic);
                }

                _context.Topics.AddRange(topicEntities);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Saved {TopicCount} AI-generated topics with PDFs for course {CourseId}", topicEntities.Count, request.CourseId);
                return Json(new { success = true, message = $"{topicEntities.Count} topics saved successfully with PDF materials!" });
            }
            catch (Exception ex)
            {
                var inner = ex.GetBaseException()?.Message ?? ex.Message;
                _logger.LogError(ex, "Error saving generated topics: {Inner}", inner);
                // Return inner exception message temporarily to aid debugging (remove in production)
                return Json(new { success = false, message = "Error: " + inner });
            }
        }
    }
}
