using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EduConnect.Data;
using EduConnect.Models;
using System.Security.Claims;

namespace EduConnect.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
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
            TempData["Success"] = "Successfully enrolled in the course!";
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

            var enrollments = await _context.Enrollments
                .Where(e => e.StudentId == userId)
                .Include(e => e.Course)
                .ThenInclude(c => c!.Materials)
                .Include(e => e.Course)
                .ThenInclude(c => c!.Quizzes)
                .ToListAsync();

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
    }
}
