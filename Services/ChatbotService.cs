using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EduConnect.Data;
using EduConnect.Models;

namespace EduConnect.Services
{
    public interface IChatbotService
    {
        Task<string> GetBotResponseAsync(string userMessage, string userId);
        Task<List<ChatMessage>> GetChatHistoryAsync(string userId);
        Task SaveChatAsync(ChatMessage message);
        Task<ChatCategory> ClassifyMessageAsync(string message);
    }

    public class ChatbotService : IChatbotService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatbotService> _logger;

        public ChatbotService(ApplicationDbContext context, ILogger<ChatbotService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GetBotResponseAsync(string userMessage, string userId)
        {
            try
            {
                var category = await ClassifyMessageAsync(userMessage);
                var response = GenerateResponse(userMessage, category);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating bot response: {ex.Message}");
                return "I'm sorry, I'm having trouble understanding. Please try again or contact support.";
            }
        }

        public async Task<List<ChatMessage>> GetChatHistoryAsync(string userId)
        {
            return await _context.ChatMessages
                .Where(m => m.UserId == userId)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task SaveChatAsync(ChatMessage message)
        {
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
        }

        public async Task<ChatCategory> ClassifyMessageAsync(string message)
        {
            var lowerMessage = message.ToLower();

            // Simple keyword-based classification
            if (lowerMessage.Contains("course") || lowerMessage.Contains("class"))
                return ChatCategory.CourseInquiry;

            if (lowerMessage.Contains("error") || lowerMessage.Contains("problem") || lowerMessage.Contains("issue") || lowerMessage.Contains("help") || lowerMessage.Contains("support"))
                return ChatCategory.TechnicalSupport;

            if (lowerMessage.Contains("enroll") || lowerMessage.Contains("register"))
                return ChatCategory.EnrollmentHelp;

            if (lowerMessage.Contains("grade") || lowerMessage.Contains("mark") || lowerMessage.Contains("score"))
                return ChatCategory.GradeInquiry;

            if (lowerMessage.Contains("material") || lowerMessage.Contains("notes") || lowerMessage.Contains("resource") || lowerMessage.Contains("download"))
                return ChatCategory.MaterialAccess;

            if (lowerMessage.Contains("quiz") || lowerMessage.Contains("test") || lowerMessage.Contains("exam"))
                return ChatCategory.QuizSupport;

            if (lowerMessage.Contains("feedback") || lowerMessage.Contains("suggest") || lowerMessage.Contains("improve"))
                return ChatCategory.Feedback;

            return ChatCategory.General;
        }

        private string GenerateResponse(string userMessage, ChatCategory category)
        {
            var lowerMessage = userMessage.ToLower();

            // Greeting responses
            if (lowerMessage.Contains("hello") || lowerMessage.Contains("hi") || lowerMessage.Contains("hey"))
                return "👋 Hello! Welcome to EduConnect. I'm here to help you with any questions about courses, enrollments, materials, quizzes, and more. How can I assist you today?";

            if (lowerMessage.Contains("thank") || lowerMessage.Contains("thanks"))
                return "😊 You're welcome! Is there anything else I can help you with?";

            if (lowerMessage.Contains("how are you") || lowerMessage.Contains("how do you do"))
                return "🤖 I'm functioning perfectly and ready to help! How can I assist you with EduConnect today?";

            // Category-specific responses
            switch (category)
            {
                case ChatCategory.CourseInquiry:
                    return "📚 Course Inquiry:\n\n" +
                           "I can help you with:\n" +
                           "• Viewing available courses\n" +
                           "• Course descriptions and schedules\n" +
                           "• Faculty information\n" +
                           "• Course prerequisites\n\n" +
                           "What specific information about courses would you like to know?";

                case ChatCategory.EnrollmentHelp:
                    return "📝 Enrollment Help:\n\n" +
                           "To enroll in a course:\n" +
                           "1. Go to 'Browse Courses'\n" +
                           "2. Select the course you want\n" +
                           "3. Click 'Enroll'\n\n" +
                           "If you're having trouble enrolling, please provide more details or contact support.";

                case ChatCategory.MaterialAccess:
                    return "📖 Material Access:\n\n" +
                           "You can access course materials by:\n" +
                           "• Going to 'My Courses'\n" +
                           "• Selecting the course\n" +
                           "• Viewing topics and materials\n" +
                           "• Downloading resources\n\n" +
                           "If you can't find materials, the faculty may not have uploaded them yet.";

                case ChatCategory.QuizSupport:
                    return "✅ Quiz Support:\n\n" +
                           "About quizzes:\n" +
                           "• Quizzes help assess your learning\n" +
                           "• You can see your scores immediately\n" +
                           "• Multiple attempts may be available\n" +
                           "• Questions types include MCQ, True/False, and Coding\n\n" +
                           "Having trouble with a quiz? Let me know!";

                case ChatCategory.GradeInquiry:
                    return "📊 Grade Information:\n\n" +
                           "Your grades are calculated from:\n" +
                           "• Course materials completion\n" +
                           "• Quiz scores\n\n" +
                           "To view your grades:\n" +
                           "1. Go to 'My Progress'\n" +
                           "2. Select a course\n" +
                           "3. See your score breakdown\n\n" +
                           "Contact your faculty for grade disputes.";

                case ChatCategory.TechnicalSupport:
                    return "🔧 Technical Support:\n\n" +
                           "Common solutions:\n" +
                           "• Clear browser cache and cookies\n" +
                           "• Try a different browser\n" +
                           "• Check your internet connection\n" +
                           "• Logout and login again\n\n" +
                           "If the problem persists, please describe the error in detail.";

                case ChatCategory.Feedback:
                    return "💬 Feedback:\n\n" +
                           "Thank you for your feedback! We appreciate suggestions to improve EduConnect.\n\n" +
                           "Your feedback helps us:\n" +
                           "• Improve user experience\n" +
                           "• Add new features\n" +
                           "• Fix issues\n\n" +
                           "Please share your feedback or suggestions below.";

                default:
                    return "👋 I'm EduConnect's AI Assistant. I can help you with:\n\n" +
                           "• 📚 Course information\n" +
                           "• 📝 Enrollment assistance\n" +
                           "• 📖 Material access\n" +
                           "• ✅ Quiz help\n" +
                           "• 📊 Grade inquiries\n" +
                           "• 🔧 Technical support\n" +
                           "• 💬 Feedback\n\n" +
                           "What would you like to know?";
            }
        }
    }
}
