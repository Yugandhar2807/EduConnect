using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EduConnect.Data;
using EduConnect.Models;
using System.Security.Claims;

namespace EduConnect.Controllers
{
    [Authorize(Roles = "Faculty,Admin")]
    public class FacultyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FacultyController(ApplicationDbContext context)
        {
            _context = context;
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
                existingCourse.Credits = course.Credits;
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
        public async Task<IActionResult> AddQuizQuestion(int quizId, string questionText, string optionA, string optionB, string optionC, string optionD, char correctOption, int marks)
        {
            var quiz = await _context.Quizzes.Include(q => q.Course).FirstOrDefaultAsync(q => q.Id == quizId);
            if (quiz == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (quiz.Course?.FacultyId != userId) return Forbid();

            if (string.IsNullOrEmpty(questionText) || string.IsNullOrEmpty(optionA) || string.IsNullOrEmpty(optionB) || 
                string.IsNullOrEmpty(optionC) || string.IsNullOrEmpty(optionD) || marks <= 0)
            {
                ModelState.AddModelError("", "Please fill in all fields properly.");
                ViewBag.QuizId = quizId;
                ViewBag.QuizTitle = quiz.Title;
                return View();
            }

            var question = new QuizQuestion
            {
                QuizId = quizId,
                QuestionText = questionText,
                OptionA = optionA,
                OptionB = optionB,
                OptionC = optionC,
                OptionD = optionD,
                CorrectOption = char.ToUpper(correctOption),
                Marks = marks
            };

            _context.Add(question);

            // Update quiz TotalQuestions and TotalMarks
            quiz.TotalQuestions += 1;
            quiz.TotalMarks += marks;
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
    }
}
