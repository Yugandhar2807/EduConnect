using System.ComponentModel.DataAnnotations;

namespace EduConnect.Models
{
    public class CourseFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Course title is required.")]
        [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [StringLength(80)]
        public string? Category { get; set; }

        [Display(Name = "Active (visible to students)")]
        public bool IsActive { get; set; } = true;
    }

    public class QuizFormViewModel
    {
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Quiz title is required.")]
        [StringLength(150)]
        public string? Title { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Passing score must be between 1 and 100 percent.")]
        [Display(Name = "Passing Score (%)")]
        public int PassingMarks { get; set; } = 50;

        [Required]
        [Range(1, 300, ErrorMessage = "Duration must be between 1 and 300 minutes.")]
        [Display(Name = "Duration (minutes)")]
        public int DurationInMinutes { get; set; } = 15;
    }

    public class QuestionFormViewModel
    {
        public int QuizId { get; set; }

        [Required(ErrorMessage = "Question text is required.")]
        [StringLength(2000)]
        [Display(Name = "Question")]
        public string? QuestionText { get; set; }

        [Required]
        [Display(Name = "Question Type")]
        public string QuestionType { get; set; } = "MCQ"; // MCQ, TrueFalse, Coding

        [StringLength(500)] public string? OptionA { get; set; }
        [StringLength(500)] public string? OptionB { get; set; }
        [StringLength(500)] public string? OptionC { get; set; }
        [StringLength(500)] public string? OptionD { get; set; }

        [Display(Name = "Correct Answer")]
        public string? CorrectOption { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Marks must be between 1 and 100.")]
        public int Marks { get; set; } = 2;

        public string? Difficulty { get; set; } = "Medium";

        // Coding questions
        [Display(Name = "Code Template")]
        public string? CodeTemplate { get; set; }

        [Display(Name = "Expected Output")]
        public string? ExpectedOutput { get; set; }

        [Display(Name = "Programming Language")]
        public string? ProgrammingLanguage { get; set; } = "python";
    }

    public class TopicFormViewModel
    {
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Topic name is required.")]
        [StringLength(200)]
        [Display(Name = "Topic Name")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(1000)]
        public string? Description { get; set; }
    }

    public class MaterialUploadViewModel
    {
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(150)]
        public string? Title { get; set; }

        [StringLength(4000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please choose the material type.")]
        [Display(Name = "Material Type")]
        public string? FileType { get; set; } // PDF, Video, Document, Image, Text
    }

    public class AnnouncementFormViewModel
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(150)]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Announcement content is required.")]
        [StringLength(4000)]
        public string? Content { get; set; }

        [Display(Name = "Course (optional — leave empty for a global announcement)")]
        public int? CourseId { get; set; }
    }
}
