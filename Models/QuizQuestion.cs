namespace EduConnect.Models
{
    public class QuizQuestion
    {
        public int Id { get; set; }
        public string? QuestionText { get; set; }
        public int QuizId { get; set; }
        
        // Multiple Choice & True/False
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public char CorrectOption { get; set; } // A, B, C, or D (for MC & TF)
        
        // Coding question fields
        public QuestionType QuestionType { get; set; } = QuestionType.MultipleChoice;
        public string? CodeTemplate { get; set; } // Template code provided to student
        public string? ExpectedOutput { get; set; } // Expected output for code question
        public string? ProgrammingLanguage { get; set; } = "csharp"; // csharp, python, javascript
        
        public int Marks { get; set; }

        public Quiz? Quiz { get; set; }
    }
}

