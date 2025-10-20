namespace EduConnect.Models
{
    public class Material
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? FilePath { get; set; }
        public string? FileType { get; set; } // PDF, Video, Document, etc.
        public int CourseId { get; set; }
        public int? TopicId { get; set; } // Optional: material can be linked to a topic
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public long FileSize { get; set; }

        public Course? Course { get; set; }
        public Topic? Topic { get; set; }
    }
}
