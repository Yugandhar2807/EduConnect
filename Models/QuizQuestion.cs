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
        public string? CorrectOption { get; set; } // A, B, C, D, True, False, or for Coding: "Requires code submission"
        
        // Question type and metadata
        public string? QuestionType { get; set; } // MCQ, TrueFalse, Coding
        public string? Difficulty { get; set; } // Easy, Medium, Hard
        public int Order { get; set; } = 1; // Question order in quiz
        
        // Coding question fields (deprecated - use QuestionType string instead)
        public QuestionType QuestionTypeEnum { get; set; }
        public string? CodeTemplate { get; set; } // Template code provided to student
        public string? ExpectedOutput { get; set; } // Expected output for code question
        public string? ProgrammingLanguage { get; set; } = "csharp"; // csharp, python, javascript
        
        public int Marks { get; set; }

        public Quiz? Quiz { get; set; }
    }
}


