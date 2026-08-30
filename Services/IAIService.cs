using EduConnect.Models;

namespace EduConnect.Services
{
    /// <summary>
    /// Interface for AI-powered content generation
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// Generate course topics based on course title and description
        /// </summary>
        Task<List<string>> GenerateTopicsAsync(string courseTitle, string courseDescription);

        /// <summary>
        /// Generate structured course topics (clean names + real descriptions,
        /// ordered from fundamentals to advanced)
        /// </summary>
        Task<List<TopicData>> GenerateStructuredTopicsAsync(string courseTitle, string courseDescription);

        /// <summary>
        /// Generate a short narrated-video script (slides with narration) for a topic
        /// </summary>
        Task<VideoScriptData?> GenerateVideoScriptAsync(string courseName, string topicName);

        /// <summary>
        /// Generate material content (description/script) for a topic
        /// </summary>
        Task<string> GenerateMaterialContentAsync(string courseName, string topicName);

        /// <summary>
        /// Generate quiz questions for a topic (mixed types: MCQ, True/False, Coding)
        /// </summary>
        Task<List<QuizQuestionData>> GenerateQuizQuestionsAsync(string courseName, string topicName, int numberOfQuestions = 5);

        /// <summary>
        /// Generate multiple-choice questions
        /// </summary>
        Task<List<QuizQuestionData>> GenerateMultipleChoiceQuestionsAsync(string courseName, string topicName, int count = 3);

        /// <summary>
        /// Generate true/false questions
        /// </summary>
        Task<List<QuizQuestionData>> GenerateTrueFalseQuestionsAsync(string courseName, string topicName, int count = 2);

        /// <summary>
        /// Generate coding challenge questions
        /// </summary>
        Task<List<QuizQuestionData>> GenerateCodingQuestionsAsync(string courseName, string topicName, int count = 1);
    }

    /// <summary>Structured topic returned by AI generation.</summary>
    public class TopicData
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>Script for a short narrated slideshow video.</summary>
    public class VideoScriptData
    {
        public string Title { get; set; } = string.Empty;
        public List<VideoSlideData> Slides { get; set; } = new();
    }

    public class VideoSlideData
    {
        public string Title { get; set; } = string.Empty;
        public List<string> Bullets { get; set; } = new();
        public string Narration { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data transfer object for quiz questions (supports all question types)
    /// </summary>
    public class QuizQuestionData
    {
        public string Question { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public string CorrectOption { get; set; } = string.Empty; // A, B, C, D, or True/False
        public int Marks { get; set; } = 1;
        public string QuestionType { get; set; } = "MCQ"; // MCQ, TrueFalse, Coding
        public string Difficulty { get; set; } = "Medium"; // Easy, Medium, Hard
    }
}
