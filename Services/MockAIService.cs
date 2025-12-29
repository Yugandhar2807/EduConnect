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
            _logger.LogInformation("Mock AI: Generating {QuestionCount} quiz questions for topic: {TopicName}", numberOfQuestions, topicName);
            await Task.Delay(1000);

            var questions = new List<QuizQuestionData>();
            
            for (int i = 1; i <= numberOfQuestions; i++)
            {
                questions.Add(new QuizQuestionData
                {
                    Question = $"What is the primary concept of {topicName} - Question {i}?",
                    OptionA = "Option related to fundamentals",
                    OptionB = "Option related to implementation",
                    OptionC = "Option related to best practices",
                    OptionD = "Option related to advanced concepts",
                    CorrectOption = (i % 4) switch
                    {
                        0 => "A",
                        1 => "B",
                        2 => "C",
                        _ => "D"
                    },
                    Marks = 1
                });
            }

            return questions;
        }
    }
}
