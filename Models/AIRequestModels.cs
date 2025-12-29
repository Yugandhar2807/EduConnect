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
}
