using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EduConnect.Models;
using EduConnect.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EduConnect.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(IChatbotService chatbotService, ILogger<ChatbotController> logger)
        {
            _chatbotService = chatbotService;
            _logger = logger;
        }

        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { error = "Message cannot be empty" });

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                // Classify the message
                var category = await _chatbotService.ClassifyMessageAsync(request.Message);

                // Get bot response
                var botResponse = await _chatbotService.GetBotResponseAsync(request.Message, userId);

                // Save user message
                var userMessage = new ChatMessage
                {
                    UserId = userId,
                    Message = request.Message,
                    Sender = MessageSender.User,
                    Response = botResponse,
                    Category = category,
                    Timestamp = DateTime.UtcNow
                };

                await _chatbotService.SaveChatAsync(userMessage);

                return Ok(new
                {
                    success = true,
                    message = userMessage.Message,
                    response = botResponse,
                    category = category.ToString(),
                    timestamp = userMessage.Timestamp
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SendMessage: {ex.Message}");
                return StatusCode(500, new { error = "An error occurred while processing your message" });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var history = await _chatbotService.GetChatHistoryAsync(userId);
                return Ok(new
                {
                    success = true,
                    messages = history
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving chat history: {ex.Message}");
                return StatusCode(500, new { error = "An error occurred while retrieving chat history" });
            }
        }

        [HttpGet("quick-suggestions")]
        public IActionResult GetQuickSuggestions()
        {
            var suggestions = new[]
            {
                "How do I enroll in a course?",
                "Where can I find course materials?",
                "How are grades calculated?",
                "I'm having technical issues",
                "What types of quizzes are available?",
                "How do I contact my faculty?"
            };

            return Ok(new
            {
                success = true,
                suggestions = suggestions
            });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }
}
