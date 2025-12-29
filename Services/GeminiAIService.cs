using System.Text.Json;
using System.Text;

namespace EduConnect.Services
{
    /// <summary>
    /// AI Service implementation using Google Gemini API via HTTP
    /// </summary>
    public class GeminiAIService : IAIService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiAIService> _logger;
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";

        public GeminiAIService(string apiKey, ILogger<GeminiAIService> logger)
        {
            _apiKey = apiKey;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Generate course topics based on course title and description
        /// </summary>
        public async Task<List<string>> GenerateTopicsAsync(string courseTitle, string courseDescription)
        {
            try
            {
                var prompt = $@"Generate 8-10 comprehensive learning topics for a course titled '{courseTitle}'. 
Description: {courseDescription}

Return ONLY a JSON array of topic titles as strings, nothing else. Example format:
[""Topic 1"", ""Topic 2"", ""Topic 3""]

Make topics progressive, building on each other from basic to advanced concepts.";

                var response = await CallGeminiAPI(prompt);
                if (string.IsNullOrEmpty(response))
                {
                    _logger.LogError("Empty response from Gemini API for course: {CourseTitle}", courseTitle);
                    return new List<string>();
                }

                _logger.LogInformation("Gemini API response: {Response}", response);

                // Try to parse JSON response
                try
                {
                    // Clean up response - remove markdown code blocks if present
                    var cleanedResponse = response.Replace("```json", "").Replace("```", "").Trim();
                    
                    var topics = JsonSerializer.Deserialize<List<string>>(cleanedResponse);
                    return topics ?? new List<string>();
                }
                catch (JsonException jex)
                {
                    _logger.LogWarning(jex, "Failed to parse topics JSON: {Response}", response);
                    return new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating topics for course: {CourseTitle}", courseTitle);
                return new List<string>();
            }
        }

        /// <summary>
        /// Generate material content (description/script) for a topic
        /// </summary>
        public async Task<string> GenerateMaterialContentAsync(string courseName, string topicName)
        {
            try
            {
                var prompt = $@"Create a comprehensive learning material for the following:
Course: {courseName}
Topic: {topicName}

Generate detailed educational content that:
1. Explains the topic clearly and concisely
2. Includes practical examples
3. Covers key concepts and sub-concepts
4. Is suitable for students learning this topic
5. Is about 500-800 words

Return clear, well-structured content suitable for a learning platform.";

                var response = await CallGeminiAPI(prompt);
                return response ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating material content for topic: {TopicName}", topicName);
                return "";
            }
        }

        /// <summary>
        /// Generate quiz questions for a topic
        /// </summary>
        public async Task<List<QuizQuestionData>> GenerateQuizQuestionsAsync(string courseName, string topicName, int numberOfQuestions = 5)
        {
            try
            {
                var prompt = $@"Generate {numberOfQuestions} multiple-choice quiz questions for the following:
Course: {courseName}
Topic: {topicName}

Return ONLY valid JSON array of questions in this exact format:
[
  {{
    ""question"": ""Question text here?"",
    ""optionA"": ""First option"",
    ""optionB"": ""Second option"",
    ""optionC"": ""Third option"",
    ""optionD"": ""Fourth option"",
    ""correctOption"": ""A"",
    ""marks"": 1
  }}
]

Requirements:
- Each question must test understanding of the topic
- Options should be plausible but clearly different
- Correct answer should vary (don't always use same option letter)
- Questions should progress from basic to intermediate difficulty
- Return ONLY the JSON array, no other text";

                var response = await CallGeminiAPI(prompt);
                if (string.IsNullOrEmpty(response)) return new List<QuizQuestionData>();

                try
                {
                    var questions = JsonSerializer.Deserialize<List<QuizQuestionData>>(response);
                    return questions ?? new List<QuizQuestionData>();
                }
                catch
                {
                    _logger.LogWarning("Failed to parse quiz questions JSON: {Response}", response);
                    return new List<QuizQuestionData>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating quiz questions for topic: {TopicName}", topicName);
                return new List<QuizQuestionData>();
            }
        }

        /// <summary>
        /// Call Gemini API with the given prompt
        /// </summary>
        private async Task<string?> CallGeminiAPI(string prompt)
        {
            try
            {
                var request = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json"
                );

                var url = $"{BaseUrl}?key={_apiKey}";
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, error);
                    throw new Exception($"API Error {response.StatusCode}: {error}");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Raw Gemini API response: {Response}", jsonResponse);

                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                // Extract text from response
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content2))
                    {
                        if (content2.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                        {
                            var firstPart = parts[0];
                            if (firstPart.TryGetProperty("text", out var text))
                            {
                                var result = text.GetString();
                                _logger.LogInformation("Extracted text from API: {Text}", result);
                                return result;
                            }
                        }
                    }
                }

                _logger.LogWarning("Could not extract text from Gemini response");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                throw;
            }
        }
    }
}
