using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EduConnect.Data;
using EduConnect.Models;
using System.Security.Claims;
using System.Text.Json;

namespace EduConnect.Controllers
{
    [Authorize(Roles = "Student")]
    public class RoadmapController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoadmapController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Browse all available roadmaps
        public async Task<IActionResult> Index()
        {
            var roadmaps = await _context.RoadmapTemplates
                .Where(r => r.IsActive)
                .OrderBy(r => r.Category)
                .ThenBy(r => r.Title)
                .ToListAsync();

            return View(roadmaps);
        }

        // View specific roadmap with flowchart
        public async Task<IActionResult> View(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var roadmap = await _context.RoadmapTemplates
                .Include(r => r.Topics)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (roadmap == null)
            {
                return NotFound();
            }

            // Verify the user exists in the database
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                // User not found, redirect to login
                return RedirectToAction("Login", "Account");
            }

            // Get or create progress for this student
            var progress = await _context.StudentRoadmapProgress
                .FirstOrDefaultAsync(p => p.StudentId == userId && p.RoadmapTemplateId == id);

            if (progress == null)
            {
                progress = new StudentRoadmapProgress
                {
                    StudentId = userId,
                    RoadmapTemplateId = id,
                    CompletedTopicIds = string.Empty,
                    ProgressPercentage = 0
                };
                _context.StudentRoadmapProgress.Add(progress);
                
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    // Log the error and show user-friendly message
                    Console.WriteLine($"Error creating progress: {ex.Message}");
                    TempData["Error"] = "Unable to track your progress. Please try again.";
                    return RedirectToAction("Index");
                }
            }

            ViewBag.Progress = progress;
            ViewBag.CompletedTopicIds = progress.CompletedTopicIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse).ToList();

            return View(roadmap);
        }

        // Get topic details (for modal)
        [HttpGet]
        public async Task<IActionResult> GetTopicDetails(int topicId)
        {
            var topic = await _context.RoadmapTopics
                .FirstOrDefaultAsync(t => t.Id == topicId);

            if (topic == null)
            {
                return NotFound();
            }

            var freeResources = string.IsNullOrEmpty(topic.FreeResources) 
                ? new List<object>() 
                : JsonSerializer.Deserialize<List<object>>(topic.FreeResources);

            var paidResources = string.IsNullOrEmpty(topic.PaidResources) 
                ? new List<object>() 
                : JsonSerializer.Deserialize<List<object>>(topic.PaidResources);

            return Json(new
            {
                title = topic.Title,
                description = topic.Description,
                freeResources = freeResources,
                paidResources = paidResources,
                aiTutorPrompt = topic.AITutorPrompt
            });
        }

        // Toggle topic completion
        [HttpPost]
        public async Task<IActionResult> ToggleTopicCompletion(int topicId, int roadmapId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var progress = await _context.StudentRoadmapProgress
                .FirstOrDefaultAsync(p => p.StudentId == userId && p.RoadmapTemplateId == roadmapId);

            if (progress == null)
            {
                return Json(new { success = false, message = "Progress not found" });
            }

            var completedIds = progress.CompletedTopicIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();

            if (completedIds.Contains(topicId))
            {
                completedIds.Remove(topicId);
            }
            else
            {
                completedIds.Add(topicId);
            }

            progress.CompletedTopicIds = string.Join(",", completedIds);

            // Calculate progress percentage
            var totalTopics = await _context.RoadmapTopics
                .CountAsync(t => t.RoadmapTemplateId == roadmapId);

            progress.ProgressPercentage = totalTopics > 0 
                ? (int)((completedIds.Count * 100.0) / totalTopics) 
                : 0;

            progress.IsCompleted = progress.ProgressPercentage == 100;
            if (progress.IsCompleted && progress.CompletedAt == null)
            {
                progress.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                isCompleted = completedIds.Contains(topicId),
                progressPercentage = progress.ProgressPercentage
            });
        }
    }
}
