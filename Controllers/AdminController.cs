using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduConnect.Data;
using EduConnect.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EduConnect.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Admin Dashboard - Analytics Overview
        public async Task<IActionResult> Dashboard()
        {
            var totalStudents = await _userManager.GetUsersInRoleAsync("Student");
            var totalFaculty = await _userManager.GetUsersInRoleAsync("Faculty");

            var viewModel = new
            {
                TotalStudents = totalStudents.Count,
                TotalFaculty = totalFaculty.Count,
                TotalCourses = await _context.Courses.CountAsync(),
                TotalEnrollments = await _context.Enrollments.CountAsync(),
                ActiveCourses = await _context.Courses.Where(c => c.IsActive).CountAsync(),
                AverageStudentProgress = await CalculateAverageStudentProgress()
            };

            return View(viewModel);
        }

        // Analytics Page - Detailed Statistics
        public async Task<IActionResult> Analytics()
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            var faculty = await _userManager.GetUsersInRoleAsync("Faculty");

            var courseStats = await _context.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.Faculty)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    FacultyName = c.Faculty.FullName,
                    EnrollmentCount = c.Enrollments.Count,
                    AverageProgress = c.Enrollments.Any() ? c.Enrollments.Average(e => e.ProgressPercentage) : 0
                })
                .ToListAsync();

            var facultyCourses = await _context.Courses
                .Include(c => c.Faculty)
                .Include(c => c.Enrollments)
                .GroupBy(c => c.FacultyId)
                .Select(g => new
                {
                    FacultyId = g.Key,
                    FacultyName = g.First().Faculty.FullName,
                    CourseCount = g.Count(),
                    StudentCount = g.SelectMany(c => c.Enrollments).Select(e => e.StudentId).Distinct().Count(),
                    Courses = g.Select(c => c.Title).ToList()
                })
                .ToListAsync();

            var analytics = new
            {
                TotalStudents = students.Count,
                TotalFaculty = faculty.Count,
                TotalCourses = await _context.Courses.CountAsync(),
                TotalEnrollments = await _context.Enrollments.CountAsync(),
                ActiveStudents = students.Where(s => s.IsActive).Count(),
                ActiveCourses = await _context.Courses.Where(c => c.IsActive).CountAsync(),
                CourseStats = courseStats,
                FacultyStats = facultyCourses,
                TopPerformingCourses = courseStats.OrderByDescending(c => c.AverageProgress).Take(5).ToList()
            };

            return View(analytics);
        }

        // Students Management - List all students
        public async Task<IActionResult> ManageStudents()
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            var studentList = new List<dynamic>();

            foreach (var student in students)
            {
                var enrollmentCount = await _context.Enrollments.Where(e => e.StudentId == student.Id).CountAsync();
                var avgProgress = await _context.Enrollments
                    .Where(e => e.StudentId == student.Id)
                    .AverageAsync(e => (double?)e.ProgressPercentage) ?? 0;

                studentList.Add(new
                {
                    student.Id,
                    student.FullName,
                    student.Email,
                    student.PhoneNumber,
                    student.IsActive,
                    EnrollmentCount = enrollmentCount,
                    AverageProgress = Math.Round(avgProgress, 2),
                    RegisteredDate = student.CreatedAt
                });
            }

            return View(studentList.OrderByDescending(s => s.RegisteredDate).ToList());
        }

        // Faculty Management - List all faculty
        public async Task<IActionResult> ManageFaculty()
        {
            var faculty = await _userManager.GetUsersInRoleAsync("Faculty");
            var facultyList = new List<dynamic>();

            foreach (var f in faculty)
            {
                var courseCount = await _context.Courses.Where(c => c.FacultyId == f.Id).CountAsync();
                var totalStudentsEnrolled = await _context.Enrollments
                    .Include(e => e.Course)
                    .Where(e => e.Course.FacultyId == f.Id)
                    .CountAsync();

                facultyList.Add(new
                {
                    f.Id,
                    f.FullName,
                    f.Email,
                    f.PhoneNumber,
                    f.Department,
                    f.IsActive,
                    CourseCount = courseCount,
                    StudentCount = totalStudentsEnrolled,
                    RegisteredDate = f.CreatedAt
                });
            }

            return View(facultyList.OrderByDescending(f => f.RegisteredDate).ToList());
        }

        // Add Student - GET
        public IActionResult AddStudent()
        {
            return View();
        }

        // Add Student - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudent(AddUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(model);
            }

            // Split FullName into FirstName and LastName
            var nameParts = model.FullName.Split(' ', 2);
            var firstName = nameParts.Length > 0 ? nameParts[0] : model.FullName;
            var lastName = nameParts.Length > 1 ? nameParts[1] : "";

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = model.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Student");
                TempData["Success"] = $"Student '{model.FullName}' added successfully!";
                return RedirectToAction("ManageStudents");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // Add Faculty - GET
        public IActionResult AddFaculty()
        {
            return View();
        }

        // Add Faculty - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFaculty(AddFacultyViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(model);
            }

            // Split FullName into FirstName and LastName
            var nameParts = model.FullName.Split(' ', 2);
            var firstName = nameParts.Length > 0 ? nameParts[0] : model.FullName;
            var lastName = nameParts.Length > 1 ? nameParts[1] : "";

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = model.PhoneNumber,
                Department = model.Department,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Faculty");
                TempData["Success"] = $"Faculty '{model.FullName}' added successfully!";
                return RedirectToAction("ManageFaculty");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // Edit Student - GET
        public async Task<IActionResult> EditStudent(string id)
        {
            var student = await _userManager.FindByIdAsync(id);
            if (student == null)
                return NotFound();

            var model = new EditUserViewModel
            {
                Id = student.Id,
                FullName = student.FullName,
                Email = student.Email,
                PhoneNumber = student.PhoneNumber,
                IsActive = student.IsActive
            };

            return View(model);
        }

        // Edit Student - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(string id, EditUserViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var student = await _userManager.FindByIdAsync(id);
            if (student == null)
                return NotFound();

            student.FullName = model.FullName;
            student.PhoneNumber = model.PhoneNumber;
            student.IsActive = model.IsActive;

            var result = await _userManager.UpdateAsync(student);
            if (result.Succeeded)
            {
                TempData["Success"] = "Student updated successfully!";
                return RedirectToAction("ManageStudents");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // Edit Faculty - GET
        public async Task<IActionResult> EditFaculty(string id)
        {
            var faculty = await _userManager.FindByIdAsync(id);
            if (faculty == null)
                return NotFound();

            var model = new EditFacultyViewModel
            {
                Id = faculty.Id,
                FullName = faculty.FullName,
                Email = faculty.Email,
                PhoneNumber = faculty.PhoneNumber,
                Department = faculty.Department,
                IsActive = faculty.IsActive
            };

            return View(model);
        }

        // Edit Faculty - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFaculty(string id, EditFacultyViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var faculty = await _userManager.FindByIdAsync(id);
            if (faculty == null)
                return NotFound();

            faculty.FullName = model.FullName;
            faculty.PhoneNumber = model.PhoneNumber;
            faculty.Department = model.Department;
            faculty.IsActive = model.IsActive;

            var result = await _userManager.UpdateAsync(faculty);
            if (result.Succeeded)
            {
                TempData["Success"] = "Faculty updated successfully!";
                return RedirectToAction("ManageFaculty");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // Delete Student
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteStudent(string id)
        {
            try
            {
                var student = await _userManager.FindByIdAsync(id);
                if (student == null)
                    return Json(new { success = false, message = "Student not found." });

                // Delete all related data first (handle cascade manually)
                // Delete TopicProgress for this student
                var topicProgress = await _context.TopicProgress.Where(tp => tp.StudentId == id).ToListAsync();
                if (topicProgress.Any())
                    _context.TopicProgress.RemoveRange(topicProgress);

                // Delete QuizResults for this student
                var quizResults = await _context.QuizResults.Where(qr => qr.StudentId == id).ToListAsync();
                if (quizResults.Any())
                    _context.QuizResults.RemoveRange(quizResults);

                // Delete Enrollments for this student
                var enrollments = await _context.Enrollments.Where(e => e.StudentId == id).ToListAsync();
                if (enrollments.Any())
                    _context.Enrollments.RemoveRange(enrollments);

                await _context.SaveChangesAsync();

                // Now delete the user
                var result = await _userManager.DeleteAsync(student);
                if (result.Succeeded)
                    return Json(new { success = true, message = "Student deleted successfully." });

                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Json(new { success = false, message = $"Error deleting student: {errors}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Delete Faculty
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteFaculty(string id)
        {
            try
            {
                var faculty = await _userManager.FindByIdAsync(id);
                if (faculty == null)
                    return Json(new { success = false, message = "Faculty not found." });

                // Get all courses created by this faculty
                var courses = await _context.Courses.Where(c => c.FacultyId == id).ToListAsync();

                foreach (var course in courses)
                {
                    // Delete all enrollments for this course
                    var enrollments = await _context.Enrollments.Where(e => e.CourseId == course.Id).ToListAsync();
                    if (enrollments.Any())
                        _context.Enrollments.RemoveRange(enrollments);

                    // Delete all topics for this course
                    var topics = await _context.Topics.Where(t => t.CourseId == course.Id).ToListAsync();
                    if (topics.Any())
                        _context.Topics.RemoveRange(topics);

                    // Delete all materials for this course
                    var materials = await _context.Materials.Where(m => m.CourseId == course.Id).ToListAsync();
                    if (materials.Any())
                        _context.Materials.RemoveRange(materials);

                    // Delete all quizzes for this course
                    var quizzes = await _context.Quizzes.Where(q => q.CourseId == course.Id).ToListAsync();
                    if (quizzes.Any())
                        _context.Quizzes.RemoveRange(quizzes);

                    // Delete all announcements for this course
                    var announcements = await _context.Announcements.Where(a => a.CourseId == course.Id).ToListAsync();
                    if (announcements.Any())
                        _context.Announcements.RemoveRange(announcements);

                    // Delete the course
                    _context.Courses.Remove(course);
                }

                // Delete announcements created by this faculty
                var facultyAnnouncements = await _context.Announcements.Where(a => a.FacultyId == id).ToListAsync();
                if (facultyAnnouncements.Any())
                    _context.Announcements.RemoveRange(facultyAnnouncements);

                await _context.SaveChangesAsync();

                // Now delete the user
                var result = await _userManager.DeleteAsync(faculty);
                if (result.Succeeded)
                    return Json(new { success = true, message = "Faculty deleted successfully." });

                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Json(new { success = false, message = $"Error deleting faculty: {errors}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Deactivate/Activate Student
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ToggleStudentStatus(string id)
        {
            var student = await _userManager.FindByIdAsync(id);
            if (student == null)
                return Json(new { success = false });

            student.IsActive = !student.IsActive;
            await _userManager.UpdateAsync(student);

            return Json(new { success = true, isActive = student.IsActive });
        }

        // Helper method to calculate average progress
        private async Task<double> CalculateAverageStudentProgress()
        {
            var enrollments = await _context.Enrollments.ToListAsync();
            if (!enrollments.Any())
                return 0;

            return Math.Round(enrollments.Average(e => e.ProgressPercentage), 2);
        }
    }

}
