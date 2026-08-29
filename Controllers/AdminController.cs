using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduConnect.Data;
using EduConnect.Models;
using EduConnect.Services;

namespace EduConnect.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private static readonly string[] Semesters = { "Fall 2025", "Spring 2026", "Summer 2026", "Fall 2026" };
        private static readonly string[] Grades = { "A", "B", "C", "D", "F" };

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IExcelExportService _excelExportService;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IExcelExportService excelExportService)
        {
            _context = context;
            _userManager = userManager;
            _excelExportService = excelExportService;
        }

        // ==================== DASHBOARD ====================

        /// <summary>Shortcut so /Admin lands on the dashboard.</summary>
        public IActionResult Index() => RedirectToAction(nameof(Dashboard));

        public async Task<IActionResult> Dashboard()
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            var facultyMembers = await _userManager.GetUsersInRoleAsync("Faculty");

            var enrollments = await _context.Enrollments.AsNoTracking().ToListAsync();
            var quizResults = await _context.QuizResults.AsNoTracking().ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalStudents = students.Count,
                TotalFaculty = facultyMembers.Count,
                TotalCourses = await _context.Courses.CountAsync(),
                ActiveCourses = await _context.Courses.CountAsync(c => c.IsActive),
                TotalEnrollments = enrollments.Count,
                TotalQuizAttempts = quizResults.Count,
                AverageStudentProgress = enrollments.Count > 0 ? Math.Round(enrollments.Average(e => e.ProgressPercentage), 1) : 0,
                QuizPassRate = quizResults.Count > 0 ? Math.Round(quizResults.Count(r => r.IsPassed) * 100.0 / quizResults.Count, 1) : 0,
            };

            // Enrollment trend over the last 6 months
            var start = DateTime.UtcNow.Date.AddMonths(-5);
            start = new DateTime(start.Year, start.Month, 1);
            for (var month = start; month <= DateTime.UtcNow.Date; month = month.AddMonths(1))
            {
                var next = month.AddMonths(1);
                model.EnrollmentsByMonth.Add(new ChartPoint(
                    month.ToString("MMM yyyy"),
                    enrollments.Count(e => e.EnrolledAt >= month && e.EnrolledAt < next)));
            }

            model.UsersByRole.Add(new ChartPoint("Students", students.Count));
            model.UsersByRole.Add(new ChartPoint("Faculty", facultyMembers.Count));

            model.TopCoursesByEnrollment = await _context.Courses.AsNoTracking()
                .Select(c => new { c.Title, Count = c.Enrollments!.Count })
                .OrderByDescending(c => c.Count)
                .Take(6)
                .Select(c => new ChartPoint(c.Title ?? "Untitled", c.Count))
                .ToListAsync();

            model.RecentEnrollments = await _context.Enrollments.AsNoTracking()
                .Include(e => e.Student)
                .Include(e => e.Course)
                .OrderByDescending(e => e.EnrolledAt)
                .Take(6)
                .Select(e => new RecentActivityItem
                {
                    StudentName = e.Student!.FullName ?? e.Student.Email,
                    Target = e.Course!.Title,
                    Detail = "Enrolled",
                    OccurredAt = e.EnrolledAt,
                })
                .ToListAsync();

            model.RecentQuizAttempts = await _context.QuizResults.AsNoTracking()
                .Include(r => r.Student)
                .Include(r => r.Quiz)
                .OrderByDescending(r => r.AttemptedAt)
                .Take(6)
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

        // ==================== ANALYTICS ====================

        public async Task<IActionResult> Analytics()
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            var facultyMembers = await _userManager.GetUsersInRoleAsync("Faculty");

            var model = new AdminAnalyticsViewModel
            {
                TotalStudents = students.Count,
                TotalFaculty = facultyMembers.Count,
                TotalCourses = await _context.Courses.CountAsync(),
                TotalEnrollments = await _context.Enrollments.CountAsync(),
                ActiveStudents = students.Count(s => s.IsActive),
            };

            model.CourseStats = await _context.Courses.AsNoTracking()
                .Select(c => new CourseStatItem
                {
                    Id = c.Id,
                    Title = c.Title,
                    Category = c.Category,
                    FacultyName = c.Faculty!.FullName ?? c.Faculty.Email,
                    EnrollmentCount = c.Enrollments!.Count,
                    AverageProgress = c.Enrollments!.Any() ? Math.Round(c.Enrollments!.Average(e => (double)e.ProgressPercentage), 1) : 0,
                    QuizCount = c.Quizzes!.Count,
                    IsActive = c.IsActive,
                })
                .OrderByDescending(c => c.EnrollmentCount)
                .ToListAsync();

            var avgScoreByCourse = await _context.QuizResults.AsNoTracking()
                .GroupBy(r => r.Quiz!.CourseId)
                .Select(g => new { CourseId = g.Key, Avg = g.Average(r => r.PercentageScore) })
                .ToDictionaryAsync(x => x.CourseId, x => Math.Round(x.Avg, 1));
            foreach (var stat in model.CourseStats)
                stat.AverageQuizScore = avgScoreByCourse.GetValueOrDefault(stat.Id);

            model.FacultyStats = await _context.Courses.AsNoTracking()
                .GroupBy(c => c.FacultyId)
                .Select(g => new FacultyStatItem
                {
                    FacultyName = g.First().Faculty!.FullName ?? g.First().Faculty!.Email,
                    Department = g.First().Faculty!.Department,
                    CourseCount = g.Count(),
                    StudentCount = g.SelectMany(c => c.Enrollments!).Select(e => e.StudentId).Distinct().Count(),
                })
                .ToListAsync();

            var semesterResults = await _context.SemesterResults.AsNoTracking().ToListAsync();
            model.AverageGpa = semesterResults.Count > 0 ? Math.Round((double)semesterResults.Average(r => r.GPA), 2) : 0;
            model.GradeDistribution = Grades
                .Select(g => new ChartPoint(g, semesterResults.Count(r => r.Grade == g)))
                .ToList();

            var attendance = await _context.Attendances.AsNoTracking()
                .Where(a => a.AttendanceDate >= DateTime.UtcNow.AddDays(-90))
                .ToListAsync();
            model.OverallAttendanceRate = attendance.Count > 0
                ? Math.Round(attendance.Count(a => a.Status == "Present") * 100.0 / attendance.Count, 1)
                : 0;
            model.AttendanceByMonth = attendance
                .GroupBy(a => new DateTime(a.AttendanceDate.Year, a.AttendanceDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new ChartPoint(
                    g.Key.ToString("MMM yyyy"),
                    Math.Round(g.Count(a => a.Status == "Present") * 100.0 / g.Count(), 1)))
                .ToList();

            return View(model);
        }

        // ==================== STUDENT MANAGEMENT ====================

        public async Task<IActionResult> ManageStudents()
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            var studentIds = students.Select(s => s.Id).ToHashSet();

            var enrollmentStats = await _context.Enrollments.AsNoTracking()
                .GroupBy(e => e.StudentId)
                .Select(g => new { StudentId = g.Key, Count = g.Count(), AvgProgress = g.Average(e => (double)e.ProgressPercentage) })
                .ToDictionaryAsync(x => x.StudentId!, x => x);

            var list = students
                .Select(s => new StudentListItemViewModel
                {
                    Id = s.Id,
                    FullName = s.FullName ?? $"{s.FirstName} {s.LastName}".Trim(),
                    Email = s.Email,
                    PhoneNumber = s.PhoneNumber,
                    IsActive = s.IsActive,
                    EnrollmentCount = enrollmentStats.TryGetValue(s.Id, out var stat) ? stat.Count : 0,
                    AverageProgress = enrollmentStats.TryGetValue(s.Id, out var st) ? Math.Round(st.AvgProgress, 1) : 0,
                    RegisteredDate = s.CreatedAt,
                })
                .OrderByDescending(s => s.RegisteredDate)
                .ToList();

            return View(list);
        }

        public async Task<IActionResult> ManageFaculty()
        {
            var facultyMembers = await _userManager.GetUsersInRoleAsync("Faculty");

            var courseStats = await _context.Courses.AsNoTracking()
                .GroupBy(c => c.FacultyId)
                .Select(g => new { FacultyId = g.Key, CourseCount = g.Count(), StudentCount = g.SelectMany(c => c.Enrollments!).Count() })
                .ToDictionaryAsync(x => x.FacultyId!, x => x);

            var list = facultyMembers
                .Select(f => new FacultyListItemViewModel
                {
                    Id = f.Id,
                    FullName = f.FullName ?? $"{f.FirstName} {f.LastName}".Trim(),
                    Email = f.Email,
                    PhoneNumber = f.PhoneNumber,
                    Department = f.Department,
                    IsActive = f.IsActive,
                    CourseCount = courseStats.TryGetValue(f.Id, out var stat) ? stat.CourseCount : 0,
                    StudentCount = courseStats.TryGetValue(f.Id, out var st) ? st.StudentCount : 0,
                    RegisteredDate = f.CreatedAt,
                })
                .OrderByDescending(f => f.RegisteredDate)
                .ToList();

            return View(list);
        }

        public IActionResult AddStudent() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudent(AddUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
                return View(model);
            }

            var user = BuildUser(model.FullName, model.Email, model.PhoneNumber, null);
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Student");
                TempData["Success"] = $"Student '{model.FullName}' added successfully.";
                return RedirectToAction(nameof(ManageStudents));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        public IActionResult AddFaculty() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFaculty(AddFacultyViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
                return View(model);
            }

            var user = BuildUser(model.FullName, model.Email, model.PhoneNumber, model.Department);
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Faculty");
                TempData["Success"] = $"Faculty member '{model.FullName}' added successfully.";
                return RedirectToAction(nameof(ManageFaculty));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        public async Task<IActionResult> EditStudent(string id)
        {
            var student = await _userManager.FindByIdAsync(id);
            if (student == null) return NotFound();

            return View(new EditUserViewModel
            {
                Id = student.Id,
                FullName = student.FullName ?? string.Empty,
                Email = student.Email,
                PhoneNumber = student.PhoneNumber,
                IsActive = student.IsActive,
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(string id, EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var student = await _userManager.FindByIdAsync(id);
            if (student == null) return NotFound();

            ApplyName(student, model.FullName);
            student.PhoneNumber = model.PhoneNumber;
            student.IsActive = model.IsActive;

            var result = await _userManager.UpdateAsync(student);
            if (result.Succeeded)
            {
                TempData["Success"] = "Student updated successfully.";
                return RedirectToAction(nameof(ManageStudents));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        public async Task<IActionResult> EditFaculty(string id)
        {
            var facultyMember = await _userManager.FindByIdAsync(id);
            if (facultyMember == null) return NotFound();

            return View(new EditFacultyViewModel
            {
                Id = facultyMember.Id,
                FullName = facultyMember.FullName ?? string.Empty,
                Email = facultyMember.Email,
                PhoneNumber = facultyMember.PhoneNumber,
                Department = facultyMember.Department ?? string.Empty,
                IsActive = facultyMember.IsActive,
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFaculty(string id, EditFacultyViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var facultyMember = await _userManager.FindByIdAsync(id);
            if (facultyMember == null) return NotFound();

            ApplyName(facultyMember, model.FullName);
            facultyMember.PhoneNumber = model.PhoneNumber;
            facultyMember.Department = model.Department;
            facultyMember.IsActive = model.IsActive;

            var result = await _userManager.UpdateAsync(facultyMember);
            if (result.Succeeded)
            {
                TempData["Success"] = "Faculty member updated successfully.";
                return RedirectToAction(nameof(ManageFaculty));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudent(string id)
        {
            try
            {
                var student = await _userManager.FindByIdAsync(id);
                if (student == null)
                    return Json(new { success = false, message = "Student not found." });

                _context.TopicProgress.RemoveRange(_context.TopicProgress.Where(tp => tp.StudentId == id));
                _context.QuizResults.RemoveRange(_context.QuizResults.Where(qr => qr.StudentId == id));
                _context.Enrollments.RemoveRange(_context.Enrollments.Where(e => e.StudentId == id));
                _context.Attendances.RemoveRange(_context.Attendances.Where(a => a.StudentId == id));
                _context.SemesterResults.RemoveRange(_context.SemesterResults.Where(sr => sr.StudentId == id));
                _context.StudentCourseProgresses.RemoveRange(_context.StudentCourseProgresses.Where(cp => cp.StudentId == id));
                await _context.SaveChangesAsync();

                var result = await _userManager.DeleteAsync(student);
                if (result.Succeeded)
                    return Json(new { success = true, message = "Student deleted successfully." });

                return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFaculty(string id)
        {
            try
            {
                var facultyMember = await _userManager.FindByIdAsync(id);
                if (facultyMember == null)
                    return Json(new { success = false, message = "Faculty member not found." });

                var courseIds = await _context.Courses
                    .Where(c => c.FacultyId == id)
                    .Select(c => c.Id)
                    .ToListAsync();

                _context.Enrollments.RemoveRange(_context.Enrollments.Where(e => courseIds.Contains(e.CourseId)));
                _context.Topics.RemoveRange(_context.Topics.Where(t => courseIds.Contains(t.CourseId)));
                _context.Materials.RemoveRange(_context.Materials.Where(m => courseIds.Contains(m.CourseId)));
                _context.Quizzes.RemoveRange(_context.Quizzes.Where(q => courseIds.Contains(q.CourseId)));
                _context.Announcements.RemoveRange(_context.Announcements.Where(a => (a.CourseId.HasValue && courseIds.Contains(a.CourseId.Value)) || a.FacultyId == id));
                _context.StudentCourseProgresses.RemoveRange(_context.StudentCourseProgresses.Where(cp => courseIds.Contains(cp.CourseId)));
                _context.Courses.RemoveRange(_context.Courses.Where(c => c.FacultyId == id));
                await _context.SaveChangesAsync();

                var result = await _userManager.DeleteAsync(facultyMember);
                if (result.Succeeded)
                    return Json(new { success = true, message = "Faculty member deleted successfully." });

                return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStudentStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return Json(new { success = false });

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            return Json(new { success = true, isActive = user.IsActive });
        }

        // ==================== SEMESTER RESULTS ====================

        [HttpGet]
        public async Task<IActionResult> SemesterResultsList(string? studentId = null, string? semester = null)
        {
            var query = _context.SemesterResults.AsNoTracking().Include(sr => sr.Student).AsQueryable();

            if (!string.IsNullOrEmpty(studentId))
                query = query.Where(sr => sr.StudentId == studentId);
            if (!string.IsNullOrEmpty(semester))
                query = query.Where(sr => sr.Semester == semester);

            var results = await query.OrderByDescending(sr => sr.CreatedAt).ToListAsync();

            ViewBag.Students = await _userManager.GetUsersInRoleAsync("Student");
            ViewBag.StudentId = studentId;
            ViewBag.Semester = semester;
            ViewBag.Semesters = Semesters;

            return View(results);
        }

        [HttpGet]
        public async Task<IActionResult> CreateSemesterResult()
        {
            await LoadSemesterFormOptions();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSemesterResult(SemesterResult model)
        {
            if (!ModelState.IsValid)
            {
                await LoadSemesterFormOptions();
                return View(model);
            }

            model.CreatedAt = DateTime.UtcNow;
            _context.SemesterResults.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Semester result recorded successfully.";
            return RedirectToAction(nameof(SemesterResultsList), new { studentId = model.StudentId });
        }

        [HttpGet]
        public async Task<IActionResult> EditSemesterResult(int id)
        {
            var result = await _context.SemesterResults
                .Include(sr => sr.Student)
                .FirstOrDefaultAsync(sr => sr.Id == id);
            if (result == null) return NotFound();

            await LoadSemesterFormOptions();
            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSemesterResult(int id, SemesterResult model)
        {
            if (id != model.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadSemesterFormOptions();
                return View(model);
            }

            var result = await _context.SemesterResults.FindAsync(id);
            if (result == null) return NotFound();

            result.Semester = model.Semester;
            result.CourseName = model.CourseName;
            result.MarksObtained = model.MarksObtained;
            result.Grade = model.Grade;
            result.GPA = model.GPA;
            result.Remarks = model.Remarks;
            result.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Semester result updated successfully.";
            return RedirectToAction(nameof(SemesterResultsList), new { studentId = result.StudentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSemesterResult(int id)
        {
            var result = await _context.SemesterResults.FindAsync(id);
            if (result == null) return NotFound();

            var studentId = result.StudentId;
            _context.SemesterResults.Remove(result);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Semester result deleted.";
            return RedirectToAction(nameof(SemesterResultsList), new { studentId });
        }

        // ==================== EXPORT ====================

        public async Task<IActionResult> DownloadStudentData()
        {
            try
            {
                var excelData = await _excelExportService.ExportStudentDataAsync();
                var fileName = $"StudentData_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error generating Excel file: " + ex.Message;
                return RedirectToAction(nameof(Dashboard));
            }
        }

        // ==================== HELPERS ====================

        private async Task LoadSemesterFormOptions()
        {
            ViewBag.Students = await _userManager.GetUsersInRoleAsync("Student");
            ViewBag.Semesters = Semesters;
            ViewBag.Grades = Grades;
        }

        private static ApplicationUser BuildUser(string fullName, string email, string? phone, string? department)
        {
            var nameParts = fullName.Split(' ', 2);
            return new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                FirstName = nameParts.Length > 0 ? nameParts[0] : fullName,
                LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty,
                PhoneNumber = phone,
                Department = department,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
        }

        private static void ApplyName(ApplicationUser user, string fullName)
        {
            user.FullName = fullName;
            var nameParts = fullName.Split(' ', 2);
            user.FirstName = nameParts.Length > 0 ? nameParts[0] : fullName;
            user.LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
        }
    }
}
