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
            if (model.QuestionTypeEnum == QuestionType.Coding)
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
                QuestionTypeEnum = model.QuestionTypeEnum,
                OptionA = model.OptionA,
                OptionB = model.OptionB,
                OptionC = model.OptionC,
                OptionD = model.OptionD,
                CorrectOption = model.CorrectOption?.ToString() ?? "A",
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

                // Create Topic entities and generate PDFs (include AI material + sample quiz in PDF)
                var topicEntities = new List<Topic>();
                var topicQuestionsList = new List<List<QuizQuestionData>>();
                foreach (var topicName in request.Topics)
                {
                    // Generate material content for the topic
                    string material = string.Empty;
                    try
                    {
                        material = await _aiService.GenerateMaterialContentAsync(course.Title, topicName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate material for topic {TopicName}", topicName);
                    }

                    // Generate mixed quiz questions for embedding in PDF
                    var questions = new List<QuizQuestionData>();
                    try
                    {
                        var mc = await _aiService.GenerateMultipleChoiceQuestionsAsync(course.Title, topicName, 3);
                        questions.AddRange(mc);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate MCQ for topic {TopicName}", topicName);
                    }

                    try
                    {
                        var tf = await _aiService.GenerateTrueFalseQuestionsAsync(course.Title, topicName, 2);
                        questions.AddRange(tf);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate True/False for topic {TopicName}", topicName);
                    }

                    try
                    {
                        var coding = await _aiService.GenerateCodingQuestionsAsync(course.Title, topicName, 1);
                        questions.AddRange(coding);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate Coding question for topic {TopicName}", topicName);
                    }

                    // Generate PDF with material and sample quiz
                    var pdfPath = await _pdfService.GenerateTopicPdfAsync(course.Title, topicName, uploadPath, material, questions);

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
                    topicQuestionsList.Add(questions);
                }

                _context.Topics.AddRange(topicEntities);
                await _context.SaveChangesAsync();

                // Create quiz entities from questions generated earlier per topic
                var quizzes = new List<Quiz>();
                for (int i = 0; i < topicEntities.Count; i++)
                {
                    var topic = topicEntities[i];
                    var questions = topicQuestionsList.Count > i ? topicQuestionsList[i] : new List<QuizQuestionData>();
                    if (questions == null || questions.Count == 0) continue;

                    _logger.LogInformation("Creating quiz entity for topic: {TopicName} with {QuestionCount} questions", topic.Name, questions.Count);

                    var quiz = new Quiz
                    {
                        Title = $"{topic.Name} - Auto-Generated Quiz",
                        Description = $"Comprehensive quiz for {topic.Name} with multiple question types",
                        CourseId = course.Id,
                        TopicId = topic.Id,
                        CreatedAt = DateTime.UtcNow,
                        Questions = new List<QuizQuestion>()
                    };

                    int questionIndex = 1;
                    foreach (var questionData in questions)
                    {
                        var quizQuestion = new QuizQuestion
                        {
                            Quiz = quiz,
                            QuestionText = questionData.Question,
                            OptionA = questionData.OptionA,
                            OptionB = questionData.OptionB,
                            OptionC = questionData.OptionC ?? string.Empty,
                            OptionD = questionData.OptionD ?? string.Empty,
                            CorrectOption = questionData.CorrectOption,
                            Marks = questionData.Marks,
                            QuestionType = questionData.QuestionType,
                            Difficulty = questionData.Difficulty,
                            Order = questionIndex++
                        };
                        quiz.Questions.Add(quizQuestion);
                    }

                    quizzes.Add(quiz);
                    _logger.LogInformation("Created auto-generated quiz for topic {TopicName} with {QuestionCount} questions", topic.Name, questions.Count);
                }

                // Save all auto-generated quizzes
                if (quizzes.Count > 0)
                {
                    _context.Quizzes.AddRange(quizzes);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Saved {QuizCount} auto-generated quizzes", quizzes.Count);
                }

                _logger.LogInformation("Saved {TopicCount} AI-generated topics with PDFs and auto-generated quizzes for course {CourseId}", 
                    topicEntities.Count, request.CourseId);
                return Json(new { 
                    success = true, 
                    message = $"{topicEntities.Count} topics saved successfully with PDF materials and {quizzes.Count} auto-generated quizzes!" 
                });
            }
            catch (Exception ex)
            {
                var inner = ex.GetBaseException()?.Message ?? ex.Message;
                _logger.LogError(ex, "Error saving generated topics: {Inner}", inner);
                // Return inner exception message temporarily to aid debugging (remove in production)
                return Json(new { success = false, message = "Error: " + inner });
            }
        }

        /// <summary>
        /// Generate questions for an existing quiz using AI based on requested counts per type.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GenerateQuestionsFromPrompt([FromBody] GenerateQuestionsRequest model)
        {
            if (model == null || model.QuizId <= 0) return Json(new { success = false, message = "QuizId is required" });

            var quiz = await _context.Quizzes.Include(q => q.Course).Include(q => q.Topic).FirstOrDefaultAsync(q => q.Id == model.QuizId);
            if (quiz == null) return Json(new { success = false, message = "Quiz not found" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (quiz.Course?.FacultyId != userId) return Json(new { success = false, message = "Unauthorized" });

            var allQuestions = new List<QuizQuestionData>();
            try
            {
                if (model.MCCount > 0)
                {
                    var mc = await _aiService.GenerateMultipleChoiceQuestionsAsync(quiz.Course.Title, quiz.Topic?.Name ?? quiz.Title, model.MCCount);
                    allQuestions.AddRange(mc);
                }
                if (model.TFCount > 0)
                {
                    var tf = await _aiService.GenerateTrueFalseQuestionsAsync(quiz.Course.Title, quiz.Topic?.Name ?? quiz.Title, model.TFCount);
                    allQuestions.AddRange(tf);
                }
                if (model.CodingCount > 0)
                {
                    var coding = await _aiService.GenerateCodingQuestionsAsync(quiz.Course.Title, quiz.Topic?.Name ?? quiz.Title, model.CodingCount);
                    allQuestions.AddRange(coding);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI generation failed for quiz {QuizId}", model.QuizId);
                return Json(new { success = false, message = "AI generation failed" });
            }

            if (allQuestions.Count == 0) return Json(new { success = false, message = "No questions generated" });

            // Create and add QuizQuestion entities
            int nextOrder = (await _context.QuizQuestions.Where(q => q.QuizId == quiz.Id).MaxAsync(q => (int?)q.Order) ) ?? 0;
            foreach (var qd in allQuestions)
            {
                nextOrder++;
                var qq = new QuizQuestion
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
                    Order = nextOrder
                };
                _context.QuizQuestions.Add(qq);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Added {allQuestions.Count} questions to quiz." });
        }

        /// <summary>
        /// Download a generated PDF for a topic
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult DownloadTopicPdf(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return NotFound();

            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "topics", fileName);
                if (!System.IO.File.Exists(filePath)) return NotFound();

                var bytes = System.IO.File.ReadAllBytes(filePath);
                return File(bytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading PDF {FileName}", fileName);
                return NotFound();
            }
        }

        // ==================== ATTENDANCE MANAGEMENT ====================

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> ManageAttendance()
        {
            var attendanceDate = DateTime.UtcNow.Date;

            // Get ONLY students (from UserManager with Student role)
            var allUsers = await _context.Users.ToListAsync();
            var allStudents = new List<ApplicationUser>();

            foreach (var user in allUsers)
            {
                var roles = await _context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                    .ToListAsync();

                if (roles.Contains("Student"))
                {
                    allStudents.Add(user);
                }
            }

            allStudents = allStudents.OrderBy(u => u.FullName ?? u.UserName).ToList();

            // Get today's attendance records
            var todayAttendance = await _context.Attendances
                .Where(a => a.AttendanceDate.Date == attendanceDate)
                .ToDictionaryAsync(a => a.StudentId, a => a.Status);

            ViewBag.AttendanceDate = attendanceDate;
            ViewBag.TodayAttendance = todayAttendance;
            return View(allStudents);
        }

        [HttpPost]
        public async Task<IActionResult> ManageAttendance(IFormCollection form)
        {
            try
            {
                var attendanceDate = DateTime.UtcNow.Date;
                var studentIds = form["studentIds"].ToList();

                foreach (var studentId in studentIds)
                {
                    var status = form[$"status_{studentId}"];
                    var remarks = form[$"remarks_{studentId}"];

                    // Skip if no status selected
                    if (string.IsNullOrEmpty(status))
                        continue;

                    // Check if student exists
                    var studentExists = await _context.Users.AnyAsync(u => u.Id == studentId);
                    if (!studentExists)
                        continue;

                    // Check if attendance already exists for today
                    var existingAttendance = await _context.Attendances
                        .FirstOrDefaultAsync(a => a.StudentId == studentId && 
                                                  a.AttendanceDate.Date == attendanceDate);

                    if (existingAttendance != null)
                    {
                        existingAttendance.Status = status;
                        existingAttendance.Remarks = remarks;
                        _context.Attendances.Update(existingAttendance);
                    }
                    else
                    {
                        var attendance = new Attendance
                        {
                            StudentId = studentId,
                            CourseId = null,
                            AttendanceDate = attendanceDate,
                            Status = status,
                            Remarks = remarks,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Attendances.Add(attendance);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Attendance saved successfully!";
                return RedirectToAction("ManageAttendance");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking attendance");
                TempData["Error"] = "Error: " + ex.Message;
                return RedirectToAction("ManageAttendance");
            }
        }

        [HttpGet]
        public async Task<IActionResult> AttendanceList()
        {
            var attendance = await _context.Attendances
                .Include(a => a.Student)
                .OrderByDescending(a => a.AttendanceDate)
                .Take(100)
                .ToListAsync();

            return View(attendance);
        }


    }
}
