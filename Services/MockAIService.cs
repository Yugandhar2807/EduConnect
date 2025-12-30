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

            // Return structured topics from beginner to advanced
            if (courseTitle.ToLower().Contains("full stack"))
            {
                return new List<string>
                {
                    "[Beginner] Frontend Fundamentals - HTML/CSS/JavaScript Basics",
                    "[Beginner] JavaScript ES6+ Essentials",
                    "[Beginner] Frontend Frameworks - Introduction to React/Vue",
                    "[Intermediate] Building Responsive Web UIs",
                    "[Intermediate] REST APIs and Data Fetching",
                    "[Intermediate] Backend Basics - Node.js/Express",
                    "[Intermediate] Database Fundamentals - SQL",
                    "[Intermediate] Authentication and Authorization",
                    "[Advanced] Full Stack Application Architecture",
                    "[Advanced] Deployment and DevOps",
                    "[Advanced] Performance Optimization and Scaling",
                    "[Advanced] Advanced Patterns and Best Practices"
                };
            }
            else if (courseTitle.ToLower().Contains("java"))
            {
                return new List<string>
                {
                    "[Beginner] Java Fundamentals and Syntax",
                    "[Beginner] Variables, Data Types, and Operators",
                    "[Beginner] Control Flow - If/Else and Loops",
                    "[Intermediate] Object-Oriented Programming Concepts",
                    "[Intermediate] Arrays and Collections",
                    "[Intermediate] Methods and Functions",
                    "[Intermediate] Exception Handling",
                    "[Advanced] File I/O and Serialization",
                    "[Advanced] Multithreading and Concurrency",
                    "[Advanced] Java 8+ Features (Streams, Lambda)",
                    "[Advanced] Design Patterns and Architectures",
                    "[Advanced] Enterprise Java and Spring Framework"
                };
            }
            else if (courseTitle.ToLower().Contains("python"))
            {
                return new List<string>
                {
                    "[Beginner] Python Installation and Setup",
                    "[Beginner] Variables, Data Types, and Operators",
                    "[Beginner] Control Structures and Loops",
                    "[Intermediate] Functions and Scope",
                    "[Intermediate] Lists, Tuples, Dictionaries, and Sets",
                    "[Intermediate] String Manipulation and Formatting",
                    "[Intermediate] File Handling and I/O",
                    "[Advanced] Object-Oriented Programming in Python",
                    "[Advanced] Functional Programming and Decorators",
                    "[Advanced] Libraries and Data Science (NumPy, Pandas)",
                    "[Advanced] Web Development with Django/Flask",
                    "[Advanced] Advanced Patterns and Performance"
                };
            }
            else if (courseTitle.ToLower().Contains("web"))
            {
                return new List<string>
                {
                    "[Beginner] HTML Fundamentals",
                    "[Beginner] CSS Styling and Layouts",
                    "[Beginner] JavaScript Basics",
                    "[Intermediate] DOM Manipulation",
                    "[Intermediate] Responsive Web Design",
                    "[Intermediate] CSS Frameworks (Bootstrap, Tailwind)",
                    "[Intermediate] JavaScript ES6+ Features",
                    "[Advanced] Asynchronous Programming (Promises, Async/Await)",
                    "[Advanced] Web APIs and Fetch",
                    "[Advanced] Frontend Frameworks (React, Vue, Angular)",
                    "[Advanced] State Management and Testing",
                    "[Advanced] Modern Web Development Tools and Bundlers"
                };
            }
            else
            {
                // Generic structured topics
                return new List<string>
                {
                    "[Beginner] Introduction and Overview",
                    "[Beginner] Core Concepts and Fundamentals",
                    "[Beginner] Basic Implementation",
                    "[Intermediate] Intermediate Techniques and Patterns",
                    "[Intermediate] Hands-On Projects",
                    "[Intermediate] Debugging and Troubleshooting",
                    "[Advanced] Advanced Topics and Deep Dives",
                    "[Advanced] Best Practices and Design Patterns",
                    "[Advanced] Real-World Applications",
                    "[Advanced] Performance Optimization and Scaling",
                    "[Advanced] Future Trends and Emerging Technologies",
                    "[Advanced] Conclusion and Next Steps"
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
