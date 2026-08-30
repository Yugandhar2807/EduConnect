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

        public Task<List<QuizQuestionData>> GenerateMultipleChoiceQuestionsAsync(string courseName, string topicName, int count = 3)
        {
            _logger.LogWarning("AI Service is not configured. Cannot generate MCQ questions.");
            return Task.FromResult(new List<QuizQuestionData>());
        }

        public Task<List<QuizQuestionData>> GenerateTrueFalseQuestionsAsync(string courseName, string topicName, int count = 2)
        {
            _logger.LogWarning("AI Service is not configured. Cannot generate True/False questions.");
            return Task.FromResult(new List<QuizQuestionData>());
        }

        public Task<List<QuizQuestionData>> GenerateCodingQuestionsAsync(string courseName, string topicName, int count = 1)
        {
            _logger.LogWarning("AI Service is not configured. Cannot generate coding questions.");
            return Task.FromResult(new List<QuizQuestionData>());
        }

        public Task<List<TopicData>> GenerateStructuredTopicsAsync(string courseTitle, string courseDescription)
        {
            _logger.LogWarning("AI Service is not configured. Cannot generate topics.");
            return Task.FromResult(new List<TopicData>());
        }

        public Task<VideoScriptData?> GenerateVideoScriptAsync(string courseName, string topicName)
        {
            _logger.LogWarning("AI Service is not configured. Cannot generate a video script.");
            return Task.FromResult<VideoScriptData?>(null);
        }
    }
}
