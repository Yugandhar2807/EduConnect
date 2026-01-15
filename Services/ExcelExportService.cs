using OfficeOpenXml;
using EduConnect.Data;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace EduConnect.Services
{
    public interface IExcelExportService
    {
        Task<byte[]> ExportStudentDataAsync();
    }

    public class ExcelExportService : IExcelExportService
    {
        private readonly ApplicationDbContext _context;

        static ExcelExportService()
        {
            // EPPlus license is set at application startup in Program.cs if needed
        }

        public ExcelExportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> ExportStudentDataAsync()
        {
            // Fetch all data
            var attendances = await _context.Attendances.Include(a => a.Student).ToListAsync();
            var courseProgress = await _context.StudentCourseProgresses.Include(cp => cp.Student).Include(cp => cp.Course).ToListAsync();
            var semesterResults = await _context.SemesterResults.Include(sr => sr.Student).ToListAsync();

            using (var package = new ExcelPackage())
            {
                // Create single worksheet
                var worksheet = package.Workbook.Worksheets.Add("Student Data");

                int currentRow = 1;

                // ==================== COMBINED HEADERS ====================
                // Row 1: Main Headers
                int col = 1;
                
                // Student Name - Attendance Headers
                worksheet.Cells[currentRow, col].Value = "Student Name";
                col++;
                worksheet.Cells[currentRow, col].Value = "Date";
                col++;
                worksheet.Cells[currentRow, col].Value = "Status";
                col++;
                worksheet.Cells[currentRow, col].Value = "Remarks";
                col++;

                // Course Progress Headers
                worksheet.Cells[currentRow, col].Value = "Course";
                col++;
                worksheet.Cells[currentRow, col].Value = "Topics Completed";
                col++;
                worksheet.Cells[currentRow, col].Value = "Total Topics";
                col++;
                worksheet.Cells[currentRow, col].Value = "Completion %";
                col++;
                worksheet.Cells[currentRow, col].Value = "Quizzes Taken";
                col++;
                worksheet.Cells[currentRow, col].Value = "Average Score";
                col++;
                worksheet.Cells[currentRow, col].Value = "Progress Status";
                col++;

                // Semester Results Headers
                worksheet.Cells[currentRow, col].Value = "Semester";
                col++;
                worksheet.Cells[currentRow, col].Value = "Course Name";
                col++;
                worksheet.Cells[currentRow, col].Value = "Marks Obtained";
                col++;
                worksheet.Cells[currentRow, col].Value = "Grade";
                col++;
                worksheet.Cells[currentRow, col].Value = "GPA";
                col++;
                worksheet.Cells[currentRow, col].Value = "Result Remarks";
                col++;

                FormatHeaderRow(worksheet, currentRow, col - 1);
                currentRow++;

                // ==================== COMBINED DATA ====================
                // Group attendance by student
                var attendanceByStudent = attendances.GroupBy(a => a.StudentId).ToList();

                foreach (var studentGroup in attendanceByStudent.OrderBy(g => g.First().Student?.FullName))
                {
                    var studentId = studentGroup.Key;
                    var studentName = studentGroup.First().Student?.FullName ?? "N/A";
                    
                    // Get course progress for this student
                    var studentCourseProgress = courseProgress.Where(cp => cp.StudentId == studentId).OrderBy(cp => cp.Course?.Title).ToList();
                    
                    // Get semester results for this student
                    var studentSemesterResults = semesterResults.Where(sr => sr.StudentId == studentId).OrderBy(sr => sr.Semester).ToList();

                    // Add row for each attendance record
                    foreach (var att in studentGroup.OrderBy(a => a.AttendanceDate))
                    {
                        col = 1;

                        // Student Name
                        worksheet.Cells[currentRow, col].Value = studentName;
                        col++;

                        // Attendance Data
                        worksheet.Cells[currentRow, col].Value = att.AttendanceDate.ToString("yyyy-MM-dd");
                        col++;
                        worksheet.Cells[currentRow, col].Value = att.Status;
                        col++;
                        worksheet.Cells[currentRow, col].Value = att.Remarks ?? "-";
                        col++;

                        // Course Progress Data (first one)
                        if (studentCourseProgress.Count > 0)
                        {
                            var cp = studentCourseProgress.First();
                            worksheet.Cells[currentRow, col].Value = cp.Course?.Title ?? "N/A";
                            col++;
                            worksheet.Cells[currentRow, col].Value = cp.TopicsCompleted;
                            col++;
                            worksheet.Cells[currentRow, col].Value = cp.TotalTopics ?? 0;
                            col++;
                            worksheet.Cells[currentRow, col].Value = cp.CompletionPercentage;
                            col++;
                            worksheet.Cells[currentRow, col].Value = cp.QuizzesTaken;
                            col++;
                            worksheet.Cells[currentRow, col].Value = cp.AverageScore;
                            col++;
                            worksheet.Cells[currentRow, col].Value = cp.ProgressStatus;
                            col++;
                        }
                        else
                        {
                            // Empty course progress columns
                            for (int i = 0; i < 7; i++)
                            {
                                worksheet.Cells[currentRow, col].Value = "-";
                                col++;
                            }
                        }

                        // Semester Results Data (first one)
                        if (studentSemesterResults.Count > 0)
                        {
                            var sr = studentSemesterResults.First();
                            worksheet.Cells[currentRow, col].Value = sr.Semester;
                            col++;
                            worksheet.Cells[currentRow, col].Value = sr.CourseName;
                            col++;
                            worksheet.Cells[currentRow, col].Value = sr.MarksObtained;
                            col++;
                            worksheet.Cells[currentRow, col].Value = sr.Grade;
                            col++;
                            worksheet.Cells[currentRow, col].Value = sr.GPA;
                            col++;
                            worksheet.Cells[currentRow, col].Value = sr.Remarks ?? "-";
                            col++;
                        }
                        else
                        {
                            // Empty semester results columns
                            for (int i = 0; i < 6; i++)
                            {
                                worksheet.Cells[currentRow, col].Value = "-";
                                col++;
                            }
                        }

                        currentRow++;
                    }
                }

                // Auto-fit all columns
                for (int i = 1; i < col; i++)
                {
                    worksheet.Column(i).AutoFit(12, 40);
                }

                return package.GetAsByteArray();
            }
        }

        private void AddSectionHeader(ExcelWorksheet worksheet, int row, string title)
        {
            worksheet.Cells[row, 1].Value = title;
            worksheet.Cells[row, 1, row, 8].Merge = true;
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1].Style.Font.Size = 14;
            worksheet.Cells[row, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            worksheet.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 78, 121)); // Dark blue
            worksheet.Cells[row, 1].Style.Font.Color.SetColor(Color.White);
            worksheet.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            worksheet.Row(row).Height = 25;
        }

        private void FormatHeaderRow(ExcelWorksheet worksheet, int row, int columnCount)
        {
            for (int col = 1; col <= columnCount; col++)
            {
                var cell = worksheet.Cells[row, col];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196)); // Blue
                cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }
            worksheet.Row(row).Height = 22;
        }
    }
}
