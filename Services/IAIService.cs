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
