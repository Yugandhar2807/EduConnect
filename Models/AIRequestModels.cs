namespace EduConnect.Models
{
    /// <summary>
    /// Request model for generating topics with AI
    /// </summary>
    public class GenerateTopicsRequest
    {
        public string? CourseTitle { get; set; }
        public string? CourseDescription { get; set; }
    }

    /// <summary>
    /// Request model for saving generated topics
    /// </summary>
    public class SaveGeneratedTopicsRequest
    {
        public int CourseId { get; set; }
        public List<string>? Topics { get; set; }
    }

    /// <summary>
    /// Request to generate questions for an existing quiz based on counts per type
    /// </summary>
    public class GenerateQuestionsRequest
    {
        public int QuizId { get; set; }
        public int MCCount { get; set; }
        public int TFCount { get; set; }
        public int CodingCount { get; set; }
        public string? Prompt { get; set; }
    }
}
