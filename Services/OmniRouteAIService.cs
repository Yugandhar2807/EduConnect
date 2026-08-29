using System.Text;
using System.Text.Json;
using EduConnect.Models;

namespace EduConnect.Services
{
    /// <summary>
    /// AI service backed by an OmniRoute gateway (https://omniroute.online) — a free,
    /// open-source AI gateway that exposes hundreds of model providers behind one
    /// OpenAI-compatible endpoint with automatic routing and fallback.
    ///
    /// Configuration (appsettings.json / environment variables):
    ///   AI:OmniRoute:Enabled  — true to use OmniRoute as the AI provider
    ///   AI:OmniRoute:BaseUrl  — gateway address (default http://localhost:20128 for a local gateway)
    ///   AI:OmniRoute:ApiKey   — bearer key if the gateway requires one (optional for local)
    ///   AI:OmniRoute:Model    — model id, e.g. "openai/gpt-4o-mini" (provider prefix optional)
    /// </summary>
    public class OmniRouteAIService : IAIService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly HttpClient _httpClient;
        private readonly ILogger<OmniRouteAIService> _logger;
        private readonly string _baseUrl;
        private readonly string? _apiKey;
        private readonly string _model;

        public OmniRouteAIService(IConfiguration configuration, ILogger<OmniRouteAIService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _baseUrl = (configuration["AI:OmniRoute:BaseUrl"] ?? "http://localhost:20128").TrimEnd('/');
            _apiKey = configuration["AI:OmniRoute:ApiKey"];
            _model = configuration["AI:OmniRoute:Model"] ?? "openai/gpt-4o-mini";

            _httpClient = httpClientFactory.CreateClient(nameof(OmniRouteAIService));
            _httpClient.Timeout = TimeSpan.FromSeconds(90);
        }

        public async Task<List<string>> GenerateTopicsAsync(string courseTitle, string courseDescription)
        {
            var prompt = $@"Generate 10-12 comprehensive learning topics for a course titled '{courseTitle}'.
Description: {courseDescription}

IMPORTANT: Structure topics from beginner to advanced. Prefix each topic with [Beginner], [Intermediate], or [Advanced].

Return ONLY a JSON array of topic title strings with level prefixes, for example:
[""[Beginner] Introduction and Basics"", ""[Intermediate] Core Concepts"", ""[Advanced] Advanced Patterns""]";

            var response = await CallOmniRouteAsync(prompt);
            return ParseJsonArray<string>(response, "topics") ?? new List<string>();
        }

        public async Task<string> GenerateMaterialContentAsync(string courseName, string topicName)
        {
            var prompt = $@"Create comprehensive learning material for the following:
Course: {courseName}
Topic: {topicName}

Generate detailed educational content that:
1. Explains the topic clearly and concisely
2. Includes practical examples
3. Covers key concepts and sub-concepts
4. Is suitable for students learning this topic
5. Is about 500-800 words

Return clear, well-structured plain text suitable for a learning platform (no markdown headers).";

            return await CallOmniRouteAsync(prompt) ?? string.Empty;
        }

        public async Task<List<QuizQuestionData>> GenerateQuizQuestionsAsync(string courseName, string topicName, int numberOfQuestions = 5)
        {
            var mcCount = Math.Max(1, numberOfQuestions / 2);
            var tfCount = Math.Max(1, (numberOfQuestions - mcCount) / 2);
            var codingCount = Math.Max(0, numberOfQuestions - mcCount - tfCount);

            var questions = new List<QuizQuestionData>();
            questions.AddRange(await GenerateMultipleChoiceQuestionsAsync(courseName, topicName, mcCount));
            questions.AddRange(await GenerateTrueFalseQuestionsAsync(courseName, topicName, tfCount));
            if (codingCount > 0)
                questions.AddRange(await GenerateCodingQuestionsAsync(courseName, topicName, codingCount));
            return questions;
        }

        public async Task<List<QuizQuestionData>> GenerateMultipleChoiceQuestionsAsync(string courseName, string topicName, int count = 3)
        {
            var prompt = $@"Generate {count} multiple-choice quiz questions for:
Course: {courseName}
Topic: {topicName}

Return ONLY a valid JSON array in this exact format (no prose, no markdown):
[
  {{
    ""question"": ""Question text here?"",
    ""optionA"": ""First option"",
    ""optionB"": ""Second option"",
    ""optionC"": ""Third option"",
    ""optionD"": ""Fourth option"",
    ""correctOption"": ""A"",
    ""marks"": 2,
    ""questionType"": ""MCQ"",
    ""difficulty"": ""Medium""
  }}
]

Requirements:
- Each question must test real understanding of the topic
- correctOption must be exactly one of A, B, C, D
- Vary difficulty between Easy, Medium and Hard";

            var response = await CallOmniRouteAsync(prompt);
            return NormalizeQuestions(ParseJsonArray<QuizQuestionData>(response, "MCQ questions"), "MCQ");
        }

        public async Task<List<QuizQuestionData>> GenerateTrueFalseQuestionsAsync(string courseName, string topicName, int count = 2)
        {
            var prompt = $@"Generate {count} true/false quiz questions for:
Course: {courseName}
Topic: {topicName}

Return ONLY a valid JSON array in this exact format (no prose, no markdown):
[
  {{
    ""question"": ""Statement that is clearly true or false."",
    ""optionA"": ""True"",
    ""optionB"": ""False"",
    ""optionC"": """",
    ""optionD"": """",
    ""correctOption"": ""True"",
    ""marks"": 2,
    ""questionType"": ""TrueFalse"",
    ""difficulty"": ""Easy""
  }}
]

correctOption must be exactly ""True"" or ""False"".";

            var response = await CallOmniRouteAsync(prompt);
            return NormalizeQuestions(ParseJsonArray<QuizQuestionData>(response, "true/false questions"), "TrueFalse");
        }

        public async Task<List<QuizQuestionData>> GenerateCodingQuestionsAsync(string courseName, string topicName, int count = 1)
        {
            // Generated as code-comprehension multiple choice ("what does this code print?")
            // so they are fully gradable — free-form AI coding tasks would need expected
            // outputs the quiz data model can't carry through this path.
            var prompt = $@"Generate {count} code-comprehension quiz questions for:
Course: {courseName}
Topic: {topicName}

Each question shows a short code snippet inside the question text and asks what it outputs or does.

Return ONLY a valid JSON array in this exact format (no prose, no markdown):
[
  {{
    ""question"": ""What does this code print?\n\ncode here"",
    ""optionA"": ""First option"",
    ""optionB"": ""Second option"",
    ""optionC"": ""Third option"",
    ""optionD"": ""Fourth option"",
    ""correctOption"": ""B"",
    ""marks"": 3,
    ""questionType"": ""MCQ"",
    ""difficulty"": ""Hard""
  }}
]";

            var response = await CallOmniRouteAsync(prompt);
            return NormalizeQuestions(ParseJsonArray<QuizQuestionData>(response, "coding questions"), "MCQ");
        }

        // ==================== internals ====================

        private async Task<string?> CallOmniRouteAsync(string prompt)
        {
            try
            {
                var payload = new
                {
                    model = _model,
                    messages = new[] { new { role = "user", content = prompt } },
                    stream = false,
                    temperature = 0.7,
                };

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/chat/completions")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
                };
                if (!string.IsNullOrWhiteSpace(_apiKey))
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OmniRoute request failed ({Status}): {Body}",
                        (int)response.StatusCode, body.Length > 500 ? body[..500] : body);
                    return null;
                }

                using var doc = JsonDocument.Parse(body);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                _logger.LogInformation("OmniRoute responded with {Length} characters (model {Model})",
                    content?.Length ?? 0, _model);
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OmniRoute gateway at {BaseUrl}", _baseUrl);
                return null;
            }
        }

        private List<T>? ParseJsonArray<T>(string? response, string what)
        {
            if (string.IsNullOrWhiteSpace(response)) return null;
            try
            {
                // Strip markdown fences and any prose around the array.
                var cleaned = response.Replace("```json", "").Replace("```", "").Trim();
                var start = cleaned.IndexOf('[');
                var end = cleaned.LastIndexOf(']');
                if (start >= 0 && end > start)
                    cleaned = cleaned[start..(end + 1)];

                return JsonSerializer.Deserialize<List<T>>(cleaned, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Could not parse OmniRoute {What} response: {Response}",
                    what, response.Length > 300 ? response[..300] : response);
                return null;
            }
        }

        private static List<QuizQuestionData> NormalizeQuestions(List<QuizQuestionData>? questions, string expectedType)
        {
            if (questions == null) return new List<QuizQuestionData>();
            foreach (var question in questions)
            {
                if (string.IsNullOrWhiteSpace(question.QuestionType))
                    question.QuestionType = expectedType;
                if (question.Marks <= 0)
                    question.Marks = 2;
                if (expectedType == "TrueFalse")
                {
                    question.OptionA = "True";
                    question.OptionB = "False";
                    if (question.CorrectOption != "True" && question.CorrectOption != "False")
                        question.CorrectOption = "True";
                }
            }
            return questions.Where(q => !string.IsNullOrWhiteSpace(q.Question)).ToList();
        }
    }
}
