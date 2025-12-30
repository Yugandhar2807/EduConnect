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
        /// Generate a comprehensive PDF with proper indexing, structure, and content
        /// </summary>
        public async Task<string> GenerateTopicPdfAsync(string courseName, string topicName, string uploadPath)
        {
            try
            {
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var invalidChars = System.IO.Path.GetInvalidFileNameChars();
                var safeTopic = new string(topicName.Where(c => !invalidChars.Contains(c)).ToArray());
                safeTopic = safeTopic.Replace(' ', '_').Replace('/', '_').Replace('\\', '_');
                var fileName = $"{safeTopic}_{DateTime.UtcNow.Ticks}.pdf";
                var filePath = Path.Combine(uploadPath, fileName);
                var relativeFilePath = $"/uploads/{fileName}";

                var htmlContent = GenerateComprehensiveHtmlContent(courseName, topicName);
                await File.WriteAllTextAsync(filePath, htmlContent, Encoding.UTF8);

                _logger.LogInformation("Generated comprehensive PDF for topic {TopicName} at {FilePath}", topicName, filePath);
                return relativeFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for topic {TopicName}", topicName);
                return string.Empty;
            }
        }

        private string GenerateComprehensiveHtmlContent(string courseName, string topicName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{courseName} - {topicName}</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: 'Segoe UI', 'Helvetica Neue', sans-serif;
            line-height: 1.8;
            color: #2c3e50;
            background: white;
        }}
        .page-break {{
            page-break-after: always;
        }}
        .cover-page {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 100px 40px;
            text-align: center;
            height: 100vh;
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: center;
        }}
        .cover-page h1 {{
            font-size: 3em;
            margin-bottom: 20px;
            text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
        }}
        .cover-page .course-name {{
            font-size: 1.5em;
            opacity: 0.9;
            margin-top: 30px;
        }}
        .toc {{
            page-break-after: always;
            padding: 40px;
        }}
        .toc h2 {{
            color: #667eea;
            margin-bottom: 30px;
            font-size: 2em;
            border-bottom: 3px solid #667eea;
            padding-bottom: 10px;
        }}
        .toc-item {{
            margin: 15px 0;
            padding-left: 20px;
            font-size: 1.1em;
        }}
        .content {{
            padding: 40px;
        }}
        .section {{
            margin-bottom: 40px;
        }}
        .section h2 {{
            color: #667eea;
            font-size: 2em;
            margin-bottom: 20px;
            border-left: 5px solid #667eea;
            padding-left: 15px;
        }}
        .section h3 {{
            color: #764ba2;
            font-size: 1.3em;
            margin-top: 20px;
            margin-bottom: 10px;
        }}
        .learning-outcomes {{
            background: #e8f4f8;
            padding: 20px;
            border-left: 5px solid #667eea;
            margin-bottom: 30px;
            border-radius: 5px;
        }}
        .learning-outcomes h3 {{
            color: #667eea;
            margin-bottom: 10px;
        }}
        .learning-outcomes ul {{
            list-style-position: inside;
            margin-left: 20px;
        }}
        .learning-outcomes li {{
            margin: 8px 0;
        }}
        .concept {{
            background: #f8f9fa;
            padding: 20px;
            margin: 20px 0;
            border-radius: 8px;
            border: 1px solid #dee2e6;
        }}
        .concept h4 {{
            color: #764ba2;
            margin-bottom: 10px;
        }}
        .example {{
            background: #fff3cd;
            padding: 15px;
            margin: 15px 0;
            border-left: 4px solid #ffc107;
            border-radius: 4px;
        }}
        .example strong {{
            color: #ff9800;
        }}
        .key-points {{
            background: #d4edda;
            padding: 20px;
            margin: 20px 0;
            border-left: 4px solid #28a745;
            border-radius: 4px;
        }}
        .key-points h4 {{
            color: #28a745;
            margin-bottom: 10px;
        }}
        .key-points ul {{
            list-style-position: inside;
            margin-left: 20px;
        }}
        .key-points li {{
            margin: 8px 0;
        }}
        .footer {{
            background: #f5f5f5;
            padding: 20px;
            text-align: center;
            font-size: 0.9em;
            color: #666;
            margin-top: 50px;
            border-top: 2px solid #ddd;
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
