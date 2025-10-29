using EduConnect.Data;
using EduConnect.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace EduConnect.Services
{
    public class RoadmapSeeder
    {
        public static async Task SeedRoadmaps(ApplicationDbContext context)
        {
            // Only seed if no roadmaps exist
            if (context.RoadmapTemplates.Any())
                return;

            var roadmaps = new List<RoadmapTemplate>();

            // 1. Python Developer Roadmap
            var pythonRoadmap = new RoadmapTemplate
            {
                Title = "Python Developer",
                Description = "Master Python from basics to advanced web development, data science, and automation",
                Category = "Programming Language",
                Level = "Beginner to Advanced",
                EstimatedHours = 120,
                Icon = "fa-python",
                Color = "#3776ab",
                IsActive = true
            };

            pythonRoadmap.Topics = new List<RoadmapTopic>
            {
                new RoadmapTopic
                {
                    Title = "Python Basics",
                    Description = "Variables, data types, control flow, functions",
                    Level = 1,
                    OrderIndex = 1,
                    PositionX = 10,
                    PositionY = 50,
                    Icon = "fa-play",
                    Color = "#3776ab",
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Python.org Official Tutorial", url = "https://docs.python.org/3/tutorial/" },
                        new { title = "FreeCodeCamp Python Course", url = "https://www.freecodecamp.org/learn/scientific-computing-with-python/" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Complete Python Bootcamp (Udemy)", url = "https://www.udemy.com/course/complete-python-bootcamp/" }
                    }),
                    AITutorPrompt = "I'm learning Python basics. Can you help me understand variables, loops, and functions?"
                },
                new RoadmapTopic
                {
                    Title = "OOP in Python",
                    Description = "Classes, inheritance, polymorphism, decorators",
                    Level = 2,
                    OrderIndex = 2,
                    PositionX = 10,
                    PositionY = 200,
                    Icon = "fa-cube",
                    Color = "#3776ab",
                    ParentTopicId = null, // Will be set after first topic is saved
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Real Python OOP", url = "https://realpython.com/python3-object-oriented-programming/" },
                        new { title = "W3Schools Python Classes", url = "https://www.w3schools.com/python/python_classes.asp" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Python OOP Masterclass", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Help me understand OOP concepts like classes, inheritance, and encapsulation in Python"
                },
                new RoadmapTopic
                {
                    Title = "Web Development",
                    Description = "Django/Flask frameworks, REST APIs, databases",
                    Level = 3,
                    OrderIndex = 3,
                    PositionX = 10,
                    PositionY = 350,
                    Icon = "fa-globe",
                    Color = "#3776ab",
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Django Official Tutorial", url = "https://docs.djangoproject.com/en/stable/intro/tutorial01/" },
                        new { title = "Flask Mega Tutorial", url = "https://blog.miguelgrinberg.com/post/the-flask-mega-tutorial-part-i-hello-world" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Django for Everybody Specialization", url = "https://www.coursera.org/" }
                    }),
                    AITutorPrompt = "I want to build web applications with Python. Should I learn Django or Flask first?"
                },
                new RoadmapTopic
                {
                    Title = "Data Science",
                    Description = "NumPy, Pandas, Matplotlib, data analysis",
                    Level = 3,
                    OrderIndex = 4,
                    PositionX = 40,
                    PositionY = 350,
                    Icon = "fa-chart-bar",
                    Color = "#3776ab",
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Kaggle Python Course", url = "https://www.kaggle.com/learn/python" },
                        new { title = "Python Data Science Handbook", url = "https://jakevdp.github.io/PythonDataScienceHandbook/" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Data Science with Python (Coursera)", url = "https://www.coursera.org/" }
                    }),
                    AITutorPrompt = "Teach me about data analysis with Python using Pandas and NumPy"
                }
            };

            // 2. Full Stack Developer Roadmap
            var fullStackRoadmap = new RoadmapTemplate
            {
                Title = "Full Stack Developer",
                Description = "Complete journey from frontend to backend development with modern technologies",
                Category = "Career Path",
                Level = "Intermediate",
                EstimatedHours = 200,
                Icon = "fa-layer-group",
                Color = "#667eea",
                IsActive = true
            };

            fullStackRoadmap.Topics = new List<RoadmapTopic>
            {
                new RoadmapTopic
                {
                    Title = "HTML & CSS",
                    Description = "Semantic HTML, CSS Grid, Flexbox, responsive design",
                    Level = 1,
                    OrderIndex = 1,
                    PositionX = 25,
                    PositionY = 50,
                    Icon = "fa-code",
                    Color = "#667eea",
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "MDN Web Docs", url = "https://developer.mozilla.org/" },
                        new { title = "FreeCodeCamp Responsive Web Design", url = "https://www.freecodecamp.org/learn/" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Advanced CSS and Sass", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Help me master HTML and CSS for modern web development"
                },
                new RoadmapTopic
                {
                    Title = "JavaScript Fundamentals",
                    Description = "ES6+, async/await, DOM manipulation",
                    Level = 2,
                    OrderIndex = 2,
                    PositionX = 25,
                    PositionY = 200,
                    Icon = "fa-js",
                    Color = "#f7df1e",
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "JavaScript.info", url = "https://javascript.info/" },
                        new { title = "Eloquent JavaScript Book", url = "https://eloquentjavascript.net/" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Complete JavaScript Course", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Explain JavaScript concepts like closures, promises, and async/await"
                },
                new RoadmapTopic
                {
                    Title = "React.js",
                    Description = "Components, hooks, state management, routing",
                    Level = 3,
                    OrderIndex = 3,
                    PositionX = 15,
                    PositionY = 350,
                    Icon = "fa-react",
                    Color = "#61dafb",
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "React Official Docs", url = "https://react.dev/" },
                        new { title = "FreeCodeCamp React Course", url = "https://www.freecodecamp.org/" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "React - The Complete Guide", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Help me learn React hooks and state management"
                },
                new RoadmapTopic
                {
                    Title = "Node.js & Express",
                    Description = "Backend development, REST APIs, middleware",
                    Level = 3,
                    OrderIndex = 4,
                    PositionX = 35,
                    PositionY = 350,
                    Icon = "fa-node",
                    Color = "#339933",
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Node.js Official Docs", url = "https://nodejs.org/docs/" },
                        new { title = "Express.js Guide", url = "https://expressjs.com/" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Node.js Complete Course", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Teach me how to build REST APIs with Node.js and Express"
                },
                new RoadmapTopic
                {
                    Title = "Databases",
                    Description = "SQL (PostgreSQL) and NoSQL (MongoDB)",
                    Level = 4,
                    OrderIndex = 5,
                    PositionX = 25,
                    PositionY = 500,
                    Icon = "fa-database",
                    Color = "#667eea",
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "PostgreSQL Tutorial", url = "https://www.postgresql.org/docs/" },
                        new { title = "MongoDB University", url = "https://university.mongodb.com/" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Complete SQL Bootcamp", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Help me understand when to use SQL vs NoSQL databases"
                }
            };

            // 3. Data Analyst Roadmap
            var dataAnalystRoadmap = new RoadmapTemplate
            {
                Title = "Data Analyst",
                Description = "Learn data analysis, visualization, and business intelligence",
                Category = "Career Path",
                Level = "Beginner to Intermediate",
                EstimatedHours = 100,
                Icon = "fa-chart-line",
                Color = "#f6993f",
                IsActive = true
            };

            dataAnalystRoadmap.Topics = new List<RoadmapTopic>
            {
                // Level 1: Excel Mastery (Parent)
                new RoadmapTopic
                {
                    Title = "Excel Mastery",
                    Description = "Master Excel for data analysis and reporting",
                    Level = 1,
                    OrderIndex = 1,
                    PositionX = 20,
                    PositionY = 50,
                    Icon = "fa-file-excel",
                    Color = "#217346",
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Excel Easy Tutorial", url = "https://www.excel-easy.com/" },
                        new { title = "Microsoft Excel Training", url = "https://support.microsoft.com/en-us/excel" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Excel Pro Tips", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Help me master Excel for data analysis"
                },
                
                // Level 2: Excel Sub-topics
                new RoadmapTopic
                {
                    Title = "Excel Formulas",
                    Description = "VLOOKUP, IF, SUMIF, INDEX-MATCH functions",
                    Level = 2,
                    OrderIndex = 1,
                    PositionX = 10,
                    PositionY = 200,
                    Icon = "fa-calculator",
                    Color = "#217346",
                    ParentTopicId = null, // Will be set after save
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Excel Formulas Guide", url = "https://www.excel-easy.com/functions.html" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Excel Formulas Masterclass", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Teach me advanced Excel formulas like VLOOKUP and INDEX-MATCH"
                },
                new RoadmapTopic
                {
                    Title = "Pivot Tables",
                    Description = "Create and analyze pivot tables for data summaries",
                    Level = 2,
                    OrderIndex = 2,
                    PositionX = 20,
                    PositionY = 200,
                    Icon = "fa-table",
                    Color = "#217346",
                    ParentTopicId = null, // Will be set after save
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Pivot Tables Tutorial", url = "https://www.excel-easy.com/data-analysis/pivot-tables.html" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Excel Pivot Tables Course", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Help me understand pivot tables for data analysis"
                },
                new RoadmapTopic
                {
                    Title = "Charts & Graphs",
                    Description = "Data visualization with Excel charts",
                    Level = 2,
                    OrderIndex = 3,
                    PositionX = 30,
                    PositionY = 200,
                    Icon = "fa-chart-line",
                    Color = "#217346",
                    ParentTopicId = null, // Will be set after save
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Excel Charts Guide", url = "https://www.excel-easy.com/examples/charts.html" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Excel Charts Mastery", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Teach me how to create effective charts in Excel"
                },
                
                // Level 3: SQL for Analysis (Parent)
                new RoadmapTopic
                {
                    Title = "SQL for Analysis",
                    Description = "Query databases for data analysis",
                    Level = 3,
                    OrderIndex = 4,
                    PositionX = 20,
                    PositionY = 350,
                    Icon = "fa-database",
                    Color = "#f6993f",
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "SQLBolt Interactive Tutorial", url = "https://sqlbolt.com/" },
                        new { title = "W3Schools SQL", url = "https://www.w3schools.com/sql/" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "The Complete SQL Bootcamp", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Teach me SQL for data analysis and reporting"
                },
                
                // Level 4: SQL Sub-topics
                new RoadmapTopic
                {
                    Title = "SQL Queries",
                    Description = "SELECT, WHERE, GROUP BY, ORDER BY",
                    Level = 4,
                    OrderIndex = 1,
                    PositionX = 10,
                    PositionY = 500,
                    Icon = "fa-code",
                    Color = "#f6993f",
                    ParentTopicId = null, // Will be set after save
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "SQL Queries Tutorial", url = "https://www.w3schools.com/sql/sql_select.asp" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "SQL Queries Masterclass", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Help me write efficient SQL queries"
                },
                new RoadmapTopic
                {
                    Title = "SQL Joins",
                    Description = "INNER, LEFT, RIGHT, FULL OUTER joins",
                    Level = 4,
                    OrderIndex = 2,
                    PositionX = 20,
                    PositionY = 500,
                    Icon = "fa-link",
                    Color = "#f6993f",
                    ParentTopicId = null, // Will be set after save
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "SQL Joins Tutorial", url = "https://www.w3schools.com/sql/sql_join.asp" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "SQL Joins Course", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Explain SQL joins with examples"
                },
                new RoadmapTopic
                {
                    Title = "Aggregations",
                    Description = "SUM, AVG, COUNT, MIN, MAX functions",
                    Level = 4,
                    OrderIndex = 3,
                    PositionX = 30,
                    PositionY = 500,
                    Icon = "fa-calculator",
                    Color = "#f6993f",
                    ParentTopicId = null, // Will be set after save
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "SQL Aggregations Guide", url = "https://www.w3schools.com/sql/sql_count_avg_sum.asp" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "SQL Aggregations Course", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Teach me SQL aggregate functions"
                },
                
                // Level 5: Data Visualization
                new RoadmapTopic
                {
                    Title = "Data Visualization",
                    Description = "Tableau, Power BI, Matplotlib, storytelling",
                    Level = 5,
                    OrderIndex = 4,
                    PositionX = 20,
                    PositionY = 650,
                    Icon = "fa-chart-pie",
                    Color = "#f6993f",
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Tableau Public Resources", url = "https://public.tableau.com/en-us/s/resources" },
                        new { title = "Power BI Learning Path", url = "https://learn.microsoft.com/en-us/power-bi/" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Data Visualization Masterclass", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Teach me data visualization best practices with Tableau and Power BI"
                }
            };

            // 4. Java Developer Roadmap
            var javaRoadmap = new RoadmapTemplate
            {
                Title = "Java Developer",
                Description = "Master Java from fundamentals to enterprise applications",
                Category = "Programming Language",
                Level = "Beginner to Advanced",
                EstimatedHours = 150,
                Icon = "fa-java",
                Color = "#f89820",
                IsActive = true
            };

            javaRoadmap.Topics = new List<RoadmapTopic>
            {
                new RoadmapTopic
                {
                    Title = "Java Fundamentals",
                    Description = "Syntax, OOP, collections, exception handling",
                    Level = 1,
                    OrderIndex = 1,
                    PositionX = 15,
                    PositionY = 50,
                    Icon = "fa-coffee",
                    Color = "#f89820",
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Oracle Java Tutorials", url = "https://docs.oracle.com/javase/tutorial/" },
                        new { title = "Java Programming MOOC", url = "https://java-programming.mooc.fi/" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Complete Java Masterclass", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Help me learn Java basics including OOP principles"
                },
                new RoadmapTopic
                {
                    Title = "Spring Framework",
                    Description = "Spring Boot, dependency injection, REST APIs",
                    Level = 2,
                    OrderIndex = 2,
                    PositionX = 15,
                    PositionY = 200,
                    Icon = "fa-leaf",
                    Color = "#6db33f",
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Spring Official Guides", url = "https://spring.io/guides" },
                        new { title = "Baeldung Spring Tutorials", url = "https://www.baeldung.com/spring-tutorial" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Spring Boot Masterclass", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Teach me Spring Boot for building enterprise applications"
                },
                new RoadmapTopic
                {
                    Title = "Microservices",
                    Description = "Spring Cloud, Docker, Kubernetes",
                    Level = 3,
                    OrderIndex = 3,
                    PositionX = 15,
                    PositionY = 350,
                    Icon = "fa-network-wired",
                    Color = "#f89820",
                    FreeResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Microservices.io", url = "https://microservices.io/" },
                        new { title = "Spring Cloud Docs", url = "https://spring.io/projects/spring-cloud" }
                    }),
                    PaidResources = JsonSerializer.Serialize(new[]
                    {
                        new { title = "Master Microservices with Spring", url = "https://www.udemy.com/" }
                    }),
                    AITutorPrompt = "Help me understand microservices architecture with Spring Cloud"
                }
            };

            roadmaps.AddRange(new[] { pythonRoadmap, fullStackRoadmap, dataAnalystRoadmap, javaRoadmap });

            context.RoadmapTemplates.AddRange(roadmaps);
            await context.SaveChangesAsync();

            // Update parent references for Python roadmap
            var savedPythonRoadmap = context.RoadmapTemplates
                .Include(r => r.Topics)
                .FirstOrDefault(r => r.Title == "Python Developer");
            
            if (savedPythonRoadmap?.Topics != null && savedPythonRoadmap.Topics.Count >= 2)
            {
                var pythonBasicsTopic = savedPythonRoadmap.Topics.FirstOrDefault(t => t.Title == "Python Basics");
                if (pythonBasicsTopic != null)
                {
                    var oopTopic = savedPythonRoadmap.Topics.FirstOrDefault(t => t.Title == "OOP in Python");
                    var webDevTopic = savedPythonRoadmap.Topics.FirstOrDefault(t => t.Title == "Web Development");
                    var dataScienceTopic = savedPythonRoadmap.Topics.FirstOrDefault(t => t.Title == "Data Science");

                    if (oopTopic != null) oopTopic.ParentTopicId = pythonBasicsTopic.Id;
                    if (webDevTopic != null) webDevTopic.ParentTopicId = oopTopic?.Id;
                    if (dataScienceTopic != null) dataScienceTopic.ParentTopicId = oopTopic?.Id;

                    await context.SaveChangesAsync();
                }
            }

            // Update parent references for Full Stack roadmap
            var savedFullStackRoadmap = context.RoadmapTemplates
                .Include(r => r.Topics)
                .FirstOrDefault(r => r.Title == "Full Stack Developer");
            
            if (savedFullStackRoadmap?.Topics != null && savedFullStackRoadmap.Topics.Count >= 2)
            {
                var htmlTopic = savedFullStackRoadmap.Topics.FirstOrDefault(t => t.Title == "HTML & CSS");
                var jsTopic = savedFullStackRoadmap.Topics.FirstOrDefault(t => t.Title == "JavaScript Fundamentals");
                var reactTopic = savedFullStackRoadmap.Topics.FirstOrDefault(t => t.Title == "React.js");
                var nodeTopic = savedFullStackRoadmap.Topics.FirstOrDefault(t => t.Title == "Node.js & Express");
                var dbTopic = savedFullStackRoadmap.Topics.FirstOrDefault(t => t.Title == "Databases");

                if (jsTopic != null) jsTopic.ParentTopicId = htmlTopic?.Id;
                if (reactTopic != null) reactTopic.ParentTopicId = jsTopic?.Id;
                if (nodeTopic != null) nodeTopic.ParentTopicId = jsTopic?.Id;
                if (dbTopic != null) dbTopic.ParentTopicId = reactTopic?.Id;

                await context.SaveChangesAsync();
            }

            // Update parent references for Data Analyst roadmap with sub-branches
            var savedDataAnalystRoadmap = context.RoadmapTemplates
                .Include(r => r.Topics)
                .FirstOrDefault(r => r.Title == "Data Analyst");
            
            if (savedDataAnalystRoadmap?.Topics != null && savedDataAnalystRoadmap.Topics.Count >= 2)
            {
                var excelTopic = savedDataAnalystRoadmap.Topics.FirstOrDefault(t => t.Title == "Excel Mastery");
                var excelFormulas = savedDataAnalystRoadmap.Topics.FirstOrDefault(t => t.Title == "Excel Formulas");
                var pivotTables = savedDataAnalystRoadmap.Topics.FirstOrDefault(t => t.Title == "Pivot Tables");
                var charts = savedDataAnalystRoadmap.Topics.FirstOrDefault(t => t.Title == "Charts & Graphs");
                
                var sqlTopic = savedDataAnalystRoadmap.Topics.FirstOrDefault(t => t.Title == "SQL for Analysis");
                var sqlQueries = savedDataAnalystRoadmap.Topics.FirstOrDefault(t => t.Title == "SQL Queries");
                var sqlJoins = savedDataAnalystRoadmap.Topics.FirstOrDefault(t => t.Title == "SQL Joins");
                var aggregations = savedDataAnalystRoadmap.Topics.FirstOrDefault(t => t.Title == "Aggregations");
                
                var visualization = savedDataAnalystRoadmap.Topics.FirstOrDefault(t => t.Title == "Data Visualization");

                // Connect Excel sub-topics to Excel parent
                if (excelFormulas != null) excelFormulas.ParentTopicId = excelTopic?.Id;
                if (pivotTables != null) pivotTables.ParentTopicId = excelTopic?.Id;
                if (charts != null) charts.ParentTopicId = excelTopic?.Id;
                
                // Connect SQL to Excel (SQL comes after Excel)
                if (sqlTopic != null) sqlTopic.ParentTopicId = excelTopic?.Id;
                
                // Connect SQL sub-topics to SQL parent
                if (sqlQueries != null) sqlQueries.ParentTopicId = sqlTopic?.Id;
                if (sqlJoins != null) sqlJoins.ParentTopicId = sqlTopic?.Id;
                if (aggregations != null) aggregations.ParentTopicId = sqlTopic?.Id;
                
                // Connect Visualization to SQL
                if (visualization != null) visualization.ParentTopicId = sqlTopic?.Id;

                await context.SaveChangesAsync();
            }

            Console.WriteLine("✅ Roadmap seed data created successfully!");
        }
    }
}
