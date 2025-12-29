namespace EduConnect.Services
{
    /// <summary>
    /// Null implementation of AI Service (when API key is not configured)
    /// </summary>
    public class NullAIService : IAIService
    {
        private readonly ILogger<NullAIService> _logger;

        public NullAIService(ILogger<NullAIService> logger)
        {
            _logger = logger;
            _logger.LogWarning("AI Service not configured - set AI:GeminiApiKey in configuration");
        }

        public Task<List<string>> GenerateTopicsAsync(string courseTitle, string courseDescription)
        {
            _logger.LogWarning("AI Service is not configured. Cannot generate topics.");
            return Task.FromResult(new List<string>());
        }

        public Task<string> GenerateMaterialContentAsync(string courseName, string topicName)
        {
            _logger.LogWarning("AI Service is not configured. Cannot generate material content.");
            return Task.FromResult("");
        }

        public Task<List<QuizQuestionData>> GenerateQuizQuestionsAsync(string courseName, string topicName, int numberOfQuestions = 5)
        {
            _logger.LogWarning("AI Service is not configured. Cannot generate quiz questions.");
            return Task.FromResult(new List<QuizQuestionData>());
        }
    }
}
