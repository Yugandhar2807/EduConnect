using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EduConnect.Services
{
    /// <summary>
    /// Mock AI Service for testing without API quota
    /// Returns pre-generated topics for demonstration
    /// </summary>
    public class MockAIService : IAIService
    {
        private readonly ILogger<MockAIService> _logger;

        public MockAIService(ILogger<MockAIService> logger)
        {
            _logger = logger;
        }

        public async Task<List<string>> GenerateTopicsAsync(string courseTitle, string courseDescription)
        {
            _logger.LogInformation("Mock AI: Generating topics for course: {CourseTitle}", courseTitle);
            await Task.Delay(1000); // Simulate API delay

            // Return topics based on course title
            if (courseTitle.ToLower().Contains("java"))
            {
                return new List<string>
                {
                    "Java Fundamentals and Syntax",
                    "Object-Oriented Programming Concepts",
                    "Data Types and Variables",
                    "Control Flow and Loops",
                    "Arrays and Collections",
                    "Methods and Functions",
                    "Exception Handling",
                    "File I/O Operations",
                    "Multithreading and Concurrency",
                    "Advanced Java Features and Best Practices"
                };
            }
            else if (courseTitle.ToLower().Contains("python"))
            {
                return new List<string>
                {
                    "Python Basics and Installation",
                    "Variables and Data Types",
                    "Operators and Expressions",
                    "Control Structures (if/elif/else)",
                    "Loops (for and while)",
                    "Functions and Scope",
                    "Lists, Tuples, and Dictionaries",
                    "String Manipulation",
                    "File Handling",
                    "Object-Oriented Programming in Python"
                };
            }
            else if (courseTitle.ToLower().Contains("web"))
            {
                return new List<string>
                {
                    "HTML Fundamentals",
                    "CSS Styling and Layouts",
                    "JavaScript Basics",
                    "DOM Manipulation",
                    "Responsive Web Design",
                    "CSS Frameworks (Bootstrap)",
                    "JavaScript ES6+ Features",
                    "Asynchronous Programming",
                    "Web APIs and Fetch",
                    "Modern Web Development Tools"
                };
            }
            else
            {
                // Generic topics
                return new List<string>
                {
                    "Introduction and Overview",
                    "Core Concepts and Fundamentals",
                    "Basic Implementation",
                    "Intermediate Techniques",
                    "Advanced Topics",
                    "Best Practices and Patterns",
                    "Real-World Applications",
                    "Troubleshooting and Debugging",
                    "Performance Optimization",
                    "Future Trends and Conclusion"
                };
            }
        }

        public async Task<string> GenerateMaterialContentAsync(string courseName, string topicName)
        {
            _logger.LogInformation("Mock AI: Generating content for topic: {TopicName}", topicName);
            await Task.Delay(1000);

            return $@"
## {topicName}

This is a comprehensive learning material for {topicName} in the course {courseName}.

### Key Concepts
- Understanding the fundamentals
- Practical applications
- Common patterns and practices
- Best strategies for learning

### Main Points
1. **Introduction**: Learn the basic concepts
2. **Core Principles**: Understand the fundamental principles
3. **Implementation**: See how to implement in practice
4. **Examples**: Review real-world examples
5. **Best Practices**: Learn industry best practices

### Practical Examples
Here are some practical examples to reinforce your understanding...

### Summary
This topic covers essential knowledge for mastering {topicName}.
Continue practicing to solidify your understanding!
";
        }

        public async Task<List<QuizQuestionData>> GenerateQuizQuestionsAsync(string courseName, string topicName, int numberOfQuestions = 5)
        {
            _logger.LogInformation("Mock AI: Generating {QuestionCount} mixed quiz questions for topic: {TopicName}", numberOfQuestions, topicName);
            await Task.Delay(1000);

            var questions = new List<QuizQuestionData>();
            
            // Mix of question types
            var mcCount = numberOfQuestions / 2;
            var tfCount = (numberOfQuestions - mcCount) / 2;
            var codingCount = numberOfQuestions - mcCount - tfCount;

            // Add Multiple Choice Questions
            for (int i = 1; i <= mcCount; i++)
            {
                questions.Add(new QuizQuestionData
                {
                    Question = $"Which of the following is a key concept in {topicName}?",
                    OptionA = "Fundamental principle approach",
                    OptionB = "Implementation strategy",
                    OptionC = "Best practice pattern",
                    OptionD = "Advanced technique",
                    CorrectOption = "C",
                    Marks = 1,
                    QuestionType = "MCQ",
                    Difficulty = "Medium"
                });
            }

            // Add True/False Questions
            for (int i = 1; i <= tfCount; i++)
            {
                questions.Add(new QuizQuestionData
                {
                    Question = $"True or False: {topicName} requires understanding of core principles.",
                    OptionA = "True",
                    OptionB = "False",
                    CorrectOption = "True",
                    Marks = 1,
                    QuestionType = "TrueFalse",
                    Difficulty = "Easy"
                });
            }

            // Add Coding Questions
            for (int i = 1; i <= codingCount; i++)
            {
                questions.Add(new QuizQuestionData
                {
                    Question = $"Write a simple implementation demonstrating a key concept of {topicName}.",
                    OptionA = "// Solution code goes here",
                    CorrectOption = "Code-based assessment",
                    Marks = 5,
                    QuestionType = "Coding",
                    Difficulty = "Hard"
                });
            }

            return questions;
        }

        public async Task<List<QuizQuestionData>> GenerateMultipleChoiceQuestionsAsync(string courseName, string topicName, int count = 3)
        {
            _logger.LogInformation("Mock AI: Generating {QuestionCount} MCQ questions for topic: {TopicName}", count, topicName);
            await Task.Delay(1000);

            var questions = new List<QuizQuestionData>();

            for (int i = 1; i <= count; i++)
            {
                questions.Add(new QuizQuestionData
                {
                    Question = $"What is the {(i == 1 ? "primary" : i == 2 ? "secondary" : "tertiary")} aspect of {topicName}?",
                    OptionA = "Foundational concept",
                    OptionB = "Practical application",
                    OptionC = "Industry standard practice",
                    OptionD = "Emerging technology trend",
                    CorrectOption = "C",
                    Marks = 1,
                    QuestionType = "MCQ",
                    Difficulty = i == 1 ? "Easy" : i == 2 ? "Medium" : "Hard"
                });
            }

            return questions;
        }

        public async Task<List<QuizQuestionData>> GenerateTrueFalseQuestionsAsync(string courseName, string topicName, int count = 2)
        {
            _logger.LogInformation("Mock AI: Generating {QuestionCount} True/False questions for topic: {TopicName}", count, topicName);
            await Task.Delay(1000);

            var questions = new List<QuizQuestionData>();

            var tfStatements = new[]
            {
                $"{topicName} is a fundamental skill in modern {courseName}.",
                $"Understanding {topicName} requires prior knowledge of related topics.",
                $"{topicName} is used in practical real-world applications.",
                $"Best practices for {topicName} have remained constant over time.",
                $"{topicName} is typically one of the harder topics to master.",
            };

            for (int i = 0; i < count && i < tfStatements.Length; i++)
            {
                questions.Add(new QuizQuestionData
                {
                    Question = tfStatements[i],
                    OptionA = "True",
                    OptionB = "False",
                    CorrectOption = i % 2 == 0 ? "True" : "False",
                    Marks = 1,
                    QuestionType = "TrueFalse",
                    Difficulty = "Easy"
                });
            }

            return questions;
        }

        public async Task<List<QuizQuestionData>> GenerateCodingQuestionsAsync(string courseName, string topicName, int count = 1)
        {
            _logger.LogInformation("Mock AI: Generating {QuestionCount} Coding questions for topic: {TopicName}", count, topicName);
            await Task.Delay(1000);

            var questions = new List<QuizQuestionData>();

            var codingChallenges = new[]
            {
                $"Implement a solution that demonstrates the core concepts of {topicName}.",
                $"Create a practical application using {topicName} principles.",
                $"Debug and optimize existing code related to {topicName}.",
                $"Design a system architecture incorporating {topicName} best practices.",
            };

            for (int i = 0; i < count && i < codingChallenges.Length; i++)
            {
                questions.Add(new QuizQuestionData
                {
                    Question = codingChallenges[i],
                    OptionA = "// Write your solution here",
                    CorrectOption = "Requires code submission and review",
                    Marks = 5,
                    QuestionType = "Coding",
                    Difficulty = i == 0 ? "Medium" : i == 1 ? "Hard" : "Hard"
                });
            }

            return questions;
        }
    }
}
