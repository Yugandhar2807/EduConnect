namespace EduConnect.Models
{
    public class QuizQuestion
    {
        public int Id { get; set; }
        public string? QuestionText { get; set; }
        public int QuizId { get; set; }
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public char CorrectOption { get; set; } // A, B, C, or D
        public int Marks { get; set; }

        public Quiz? Quiz { get; set; }
    }
}
