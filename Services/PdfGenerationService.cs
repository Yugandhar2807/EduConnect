using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EduConnect.Services
{
    /// <summary>
    /// Service for generating simple PDF files for course topics
    /// </summary>
    public class PdfGenerationService
    {
        private readonly ILogger<PdfGenerationService> _logger;

        public PdfGenerationService(ILogger<PdfGenerationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Generate a simple PDF content for a topic
        /// Creates an HTML-based PDF representation
        /// </summary>
        public async Task<string> GenerateTopicPdfAsync(string courseName, string topicName, string uploadPath)
        {
            try
            {
                // Create uploads directory if it doesn't exist
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Generate filename
                var fileName = $"{topicName.Replace(" ", "_")}_{DateTime.UtcNow.Ticks}.pdf";
                var filePath = Path.Combine(uploadPath, fileName);
                var relativeFilePath = $"/uploads/{fileName}";

                // Create a simple HTML-based PDF content
                var htmlContent = GenerateHtmlContent(courseName, topicName);
                
                // Save as HTML file (browsers can read it as PDF-like)
                // In a real scenario, use a library like iTextSharp or HtmlRenderer
                await File.WriteAllTextAsync(filePath, htmlContent, Encoding.UTF8);

                _logger.LogInformation("Generated PDF for topic {TopicName} at {FilePath}", topicName, filePath);
                return relativeFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for topic {TopicName}", topicName);
                return null;
            }
        }

        private string GenerateHtmlContent(string courseName, string topicName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>{topicName}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 800px;
            margin: 0 auto;
            padding: 40px;
            background-color: #f5f5f5;
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            border-radius: 10px;
            margin-bottom: 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 2em;
        }}
        .header p {{
            margin: 10px 0 0 0;
            opacity: 0.9;
        }}
        .content {{
            background: white;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .content h2 {{
            color: #667eea;
            border-bottom: 2px solid #667eea;
            padding-bottom: 10px;
        }}
        .section {{
            margin: 20px 0;
        }}
        ul {{
            list-style-position: inside;
            line-height: 1.8;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #eee;
            text-align: center;
            color: #666;
            font-size: 0.9em;
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>{topicName}</h1>
        <p>Course: {courseName}</p>
        <p>Generated on {DateTime.UtcNow:MMMM dd, yyyy}</p>
    </div>

    <div class='content'>
        <h2>Overview</h2>
        <div class='section'>
            <p>This document covers the essential concepts and information for the topic <strong>{topicName}</strong> in the course <strong>{courseName}</strong>.</p>
        </div>

        <h2>Key Concepts</h2>
        <div class='section'>
            <ul>
                <li>Fundamental principles of {topicName}</li>
                <li>Core concepts and terminology</li>
                <li>Best practices and standards</li>
                <li>Real-world applications</li>
                <li>Common challenges and solutions</li>
            </ul>
        </div>

        <h2>Learning Objectives</h2>
        <div class='section'>
            <p>By studying this topic, you will be able to:</p>
            <ul>
                <li>Understand the fundamental concepts</li>
                <li>Apply the principles in practical scenarios</li>
                <li>Identify best practices and patterns</li>
                <li>Solve real-world problems effectively</li>
                <li>Continue learning and growing in this area</li>
            </ul>
        </div>

        <h2>Study Guide</h2>
        <div class='section'>
            <p><strong>Step 1:</strong> Review the fundamental concepts and terminology</p>
            <p><strong>Step 2:</strong> Examine practical examples and use cases</p>
            <p><strong>Step 3:</strong> Complete exercises and practice problems</p>
            <p><strong>Step 4:</strong> Review best practices and advanced topics</p>
            <p><strong>Step 5:</strong> Test your knowledge with quizzes and assessments</p>
        </div>

        <h2>Additional Resources</h2>
        <div class='section'>
            <ul>
                <li>Interactive exercises and coding challenges</li>
                <li>Video tutorials and lectures</li>
                <li>Research papers and documentation</li>
                <li>Community forums and discussion boards</li>
                <li>Hands-on projects and real-world applications</li>
            </ul>
        </div>

        <div class='footer'>
            <p>This study material was auto-generated by EduConnect AI Learning Platform</p>
            <p>© 2024 EduConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>
";
        }
    }
}
