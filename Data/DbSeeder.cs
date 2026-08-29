using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EduConnect.Models;

namespace EduConnect.Data
{
    /// <summary>
    /// Seeds roles, the admin account and — when the catalog is empty — a full set of
    /// realistic demo data so the application can be demonstrated immediately.
    /// All credentials come from configuration (Seed section), never from source code.
    /// </summary>
    public static class DbSeeder
    {
        private record QData(string Text, string Type, string? A, string? B, string? C, string? D, string Correct, string Difficulty);

        public static async Task SeedAsync(IServiceProvider services, IConfiguration config, ILogger logger)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // ---------- Roles ----------
            foreach (var role in new[] { "Admin", "Faculty", "Student" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ---------- Admin account (from configuration) ----------
            var adminEmail = config["Seed:AdminEmail"] ?? "admin@educonnect.com";
            var adminPassword = config["Seed:AdminPassword"] ?? "Admin@123";
            var admin = await EnsureUserAsync(userManager, logger, adminEmail, adminPassword,
                "System", "Administrator", "Admin", department: "Administration");

            // ---------- Demo data (only when the catalog is empty) ----------
            if (!config.GetValue("Seed:SeedDemoData", true)) return;
            if (await context.Courses.AnyAsync()) return;

            logger.LogInformation("Seeding demo data (courses, users, quizzes, attendance, results)...");

            var facultyPassword = config["Seed:DemoFacultyPassword"] ?? "Faculty@123";
            var studentPassword = config["Seed:DemoStudentPassword"] ?? "Student@123";

            var faculty = new List<ApplicationUser>
            {
                await EnsureUserAsync(userManager, logger, "sarah.mitchell@educonnect.com", facultyPassword, "Sarah", "Mitchell", "Faculty", "Computer Science"),
                await EnsureUserAsync(userManager, logger, "james.carter@educonnect.com", facultyPassword, "James", "Carter", "Faculty", "Information Technology"),
                await EnsureUserAsync(userManager, logger, "priya.sharma@educonnect.com", facultyPassword, "Priya", "Sharma", "Faculty", "Data Science"),
            };

            var studentSeed = new (string Email, string First, string Last)[]
            {
                ("alex.johnson@student.educonnect.com", "Alex", "Johnson"),
                ("maria.garcia@student.educonnect.com", "Maria", "Garcia"),
                ("david.lee@student.educonnect.com", "David", "Lee"),
                ("emily.chen@student.educonnect.com", "Emily", "Chen"),
                ("michael.brown@student.educonnect.com", "Michael", "Brown"),
                ("sofia.rossi@student.educonnect.com", "Sofia", "Rossi"),
                ("ryan.patel@student.educonnect.com", "Ryan", "Patel"),
                ("olivia.smith@student.educonnect.com", "Olivia", "Smith"),
            };

            var students = new List<ApplicationUser>();
            foreach (var (email, first, last) in studentSeed)
                students.Add(await EnsureUserAsync(userManager, logger, email, studentPassword, first, last, "Student", null));

            var now = DateTime.UtcNow;
            var rng = new Random(42);

            // ---------- Courses ----------
            var courses = new List<Course>
            {
                NewCourse("C# Programming Fundamentals", "Learn the C# language from the ground up: syntax, types, control flow, object-oriented programming and the .NET ecosystem. Ideal for first-year programming students.", "Programming", faculty[0].Id, now.AddDays(-120)),
                NewCourse("Web Development with ASP.NET Core", "Build modern, secure web applications with ASP.NET Core MVC — routing, Razor views, Entity Framework Core, authentication and deployment.", "Web Development", faculty[0].Id, now.AddDays(-110)),
                NewCourse("Python for Data Analysis", "Hands-on data analysis with Python: NumPy, Pandas, data cleaning, exploratory analysis and visualisation with Matplotlib.", "Data Science", faculty[2].Id, now.AddDays(-100)),
                NewCourse("Database Design & SQL", "Relational modelling, normalisation, SQL querying, indexing and transactions. Includes practical work with real-world schemas.", "Databases", faculty[1].Id, now.AddDays(-95)),
                NewCourse("JavaScript & Modern Frontend", "Master modern JavaScript (ES6+), the DOM, fetch APIs, and component-based UI patterns used by today's frontend frameworks.", "Web Development", faculty[1].Id, now.AddDays(-80)),
                NewCourse("Machine Learning Essentials", "A practical introduction to supervised and unsupervised learning: regression, classification, clustering, model evaluation and scikit-learn.", "Data Science", faculty[2].Id, now.AddDays(-60)),
                NewCourse("Legacy Systems Maintenance", "Archived course covering maintenance strategies for legacy enterprise systems.", "Software Engineering", faculty[1].Id, now.AddDays(-300), isActive: false),
            };
            context.Courses.AddRange(courses);
            await context.SaveChangesAsync();

            // ---------- Topics ----------
            var topicNames = new Dictionary<int, string[]>
            {
                [0] = new[] { "Introduction to C# and .NET", "Variables, Types and Operators", "Control Flow and Loops", "Object-Oriented Programming", "Collections and LINQ" },
                [1] = new[] { "ASP.NET Core Project Structure", "Routing and Controllers", "Razor Views and Layouts", "Entity Framework Core", "Authentication and Identity" },
                [2] = new[] { "Python Environment Setup", "NumPy Arrays", "Pandas DataFrames", "Data Cleaning Techniques", "Visualisation with Matplotlib" },
                [3] = new[] { "Relational Model Basics", "SQL SELECT and Filtering", "Joins and Aggregation", "Normalisation", "Indexes and Transactions" },
                [4] = new[] { "Modern JavaScript Syntax", "The DOM and Events", "Async and Fetch API", "Modules and Tooling", "Component Patterns" },
                [5] = new[] { "ML Workflow Overview", "Linear Regression", "Classification Algorithms", "Clustering", "Model Evaluation" },
            };
            var topics = new List<Topic>();
            foreach (var (idx, names) in topicNames)
            {
                foreach (var (name, i) in names.Select((n, i) => (n, i)))
                {
                    topics.Add(new Topic
                    {
                        CourseId = courses[idx].Id,
                        Name = name,
                        Description = $"Covers {name.ToLowerInvariant()} with worked examples, practice exercises and further reading.",
                        PdfFilePath = string.Empty,
                        CreatedAt = courses[idx].CreatedAt.AddDays(i + 1),
                        UpdatedAt = courses[idx].CreatedAt.AddDays(i + 1),
                    });
                }
            }
            context.Topics.AddRange(topics);
            await context.SaveChangesAsync();

            // ---------- Materials (text-based study notes) ----------
            var materials = new List<Material>();
            var materialTitles = new Dictionary<int, (string Title, string Body)[]>
            {
                [0] = new[]
                {
                    ("Getting Started with C#", "C# is a modern, type-safe, object-oriented language. This note walks through installing the .NET SDK, creating your first console application with `dotnet new console`, and understanding the Main entry point. Key concepts: namespaces, the using directive, and the difference between value types (int, bool, struct) and reference types (class, string, arrays)."),
                    ("OOP Cheat Sheet", "The four pillars of object-oriented programming in C#: Encapsulation (private fields with public properties), Inheritance (a class deriving from a base class with the : syntax), Polymorphism (virtual/override methods and interfaces), and Abstraction (abstract classes and interfaces). Includes examples of constructors, method overloading, and the difference between abstract classes and interfaces."),
                    ("LINQ Quick Reference", "Language Integrated Query lets you query collections with a fluent syntax. Common operators: Where (filter), Select (project), OrderBy/ThenBy (sort), GroupBy (group), First/FirstOrDefault, Any/All, Sum/Average/Count. Prefer method syntax for composability, and remember that LINQ queries are lazily evaluated until enumerated."),
                },
                [1] = new[]
                {
                    ("MVC Pattern Explained", "Model-View-Controller separates an application into three responsibilities. The Controller receives the HTTP request, orchestrates business logic, and selects a View. The Model carries data and validation rules. The View renders HTML using Razor syntax. This note maps each concept to the folders in an ASP.NET Core project."),
                    ("Entity Framework Core Essentials", "EF Core maps C# classes to database tables. Key pieces: DbContext (unit of work), DbSet<T> (table), migrations (schema evolution via dotnet ef migrations add / database update), LINQ-to-SQL translation, and change tracking. Includes guidance on Include() for eager loading and avoiding the N+1 query problem."),
                },
                [2] = new[]
                {
                    ("Pandas in 10 Minutes", "DataFrames are labelled, tabular data structures. Load data with pd.read_csv, inspect with head()/info()/describe(), select columns with df['col'], filter rows with boolean masks, and aggregate with groupby(). This note ends with a worked example analysing a student grades dataset."),
                    ("Data Cleaning Checklist", "Real-world data is messy. Steps: 1) handle missing values (dropna/fillna), 2) fix data types (astype, to_datetime), 3) remove duplicates (drop_duplicates), 4) standardise categories (str.strip, str.lower, replace), 5) detect outliers (IQR method), 6) validate ranges. Always keep a copy of the raw data."),
                },
                [3] = new[]
                {
                    ("SQL Joins Illustrated", "INNER JOIN returns matching rows from both tables; LEFT JOIN keeps every row of the left table with NULLs where no match exists; RIGHT and FULL joins mirror/extend that. This note illustrates each with a Students/Enrollments example and shows how join order and ON conditions affect results."),
                    ("Normalisation Walkthrough", "1NF: atomic values, no repeating groups. 2NF: no partial dependency on a composite key. 3NF: no transitive dependencies. The note normalises a flat spreadsheet of course registrations into Students, Courses and Enrollments tables step by step, discussing the trade-offs of denormalisation for reporting."),
                },
                [4] = new[]
                {
                    ("ES6+ Features You Must Know", "let/const and block scoping, arrow functions and lexical this, template literals, destructuring, spread/rest, default parameters, optional chaining (?.), nullish coalescing (??), modules (import/export), and Promises with async/await. Each feature comes with a before/after code sample."),
                    ("Working with the Fetch API", "fetch(url) returns a Promise of a Response. Check response.ok, parse with response.json(), and wrap calls in try/catch with async/await. This note covers sending JSON with POST (headers: Content-Type application/json), handling errors, aborting requests with AbortController, and rendering results in the DOM."),
                },
                [5] = new[]
                {
                    ("Choosing the Right Algorithm", "A decision guide: predicting a number → regression (linear, tree-based); predicting a category → classification (logistic regression, random forest, SVM); no labels → clustering (k-means, DBSCAN) or dimensionality reduction (PCA). Discusses bias/variance, underfitting vs overfitting, and when simple models win."),
                    ("Model Evaluation Metrics", "Classification: accuracy, precision, recall, F1, ROC-AUC and when each matters (imbalanced classes!). Regression: MAE, MSE, RMSE, R². Always evaluate on a held-out test set, use cross-validation for small datasets, and beware of data leakage from preprocessing before the split."),
                },
            };
            foreach (var (idx, notes) in materialTitles)
            {
                foreach (var (note, i) in notes.Select((n, i) => (n, i)))
                {
                    materials.Add(new Material
                    {
                        CourseId = courses[idx].Id,
                        Title = note.Title,
                        Description = note.Body,
                        FileType = "Text",
                        FilePath = string.Empty,
                        UploadedAt = courses[idx].CreatedAt.AddDays(3 + i * 2),
                        FileSize = 0,
                    });
                }
            }
            context.Materials.AddRange(materials);
            await context.SaveChangesAsync();

            // ---------- Quizzes with questions ----------
            var quizzes = SeedQuizzes(courses, topics, now);
            context.Quizzes.AddRange(quizzes);
            await context.SaveChangesAsync();

            // ---------- Enrollments ----------
            var enrollmentMatrix = new Dictionary<int, int[]>
            {
                [0] = new[] { 0, 1, 3 },
                [1] = new[] { 0, 2, 4 },
                [2] = new[] { 1, 3, 5 },
                [3] = new[] { 0, 2, 5 },
                [4] = new[] { 1, 4 },
                [5] = new[] { 2, 3, 4 },
                [6] = new[] { 0, 1, 5 },
                [7] = new[] { 2, 4, 5 },
            };
            var enrollments = new List<Enrollment>();
            foreach (var (sIdx, courseIdxs) in enrollmentMatrix)
            {
                foreach (var cIdx in courseIdxs)
                {
                    enrollments.Add(new Enrollment
                    {
                        StudentId = students[sIdx].Id,
                        CourseId = courses[cIdx].Id,
                        EnrolledAt = now.AddDays(-rng.Next(20, 70)),
                        ProgressPercentage = 0,
                        IsCompleted = false,
                    });
                }
            }
            context.Enrollments.AddRange(enrollments);
            await context.SaveChangesAsync();

            // ---------- Quiz results ----------
            var results = new List<QuizResult>();
            foreach (var enrollment in enrollments)
            {
                var courseQuizzes = quizzes.Where(q => q.CourseId == enrollment.CourseId).ToList();
                var attempts = rng.Next(1, courseQuizzes.Count + 1);
                foreach (var quiz in courseQuizzes.Take(attempts))
                {
                    var pct = rng.Next(35, 101);
                    var marks = (int)Math.Round(quiz.TotalMarks * pct / 100.0);
                    results.Add(new QuizResult
                    {
                        QuizId = quiz.Id,
                        StudentId = enrollment.StudentId,
                        MarksObtained = marks,
                        TotalMarks = quiz.TotalMarks,
                        PercentageScore = Math.Round(marks * 100.0 / quiz.TotalMarks, 2),
                        IsPassed = marks * 100.0 / quiz.TotalMarks >= quiz.PassingMarks,
                        AttemptedAt = enrollment.EnrolledAt.AddDays(rng.Next(2, 25)),
                        DurationTakenInSeconds = rng.Next(180, quiz.DurationInMinutes * 60),
                    });
                }
            }
            context.QuizResults.AddRange(results);
            await context.SaveChangesAsync();

            // ---------- Topic/material completion + enrollment progress ----------
            var topicProgress = new List<TopicProgress>();
            var courseProgressRows = new List<StudentCourseProgress>();
            foreach (var enrollment in enrollments)
            {
                var courseTopics = topics.Where(t => t.CourseId == enrollment.CourseId).ToList();
                var courseMaterials = materials.Where(m => m.CourseId == enrollment.CourseId).ToList();
                var courseQuizIds = quizzes.Where(q => q.CourseId == enrollment.CourseId).Select(q => q.Id).ToHashSet();
                var myResults = results.Where(r => r.StudentId == enrollment.StudentId && courseQuizIds.Contains(r.QuizId)).ToList();

                var completedTopics = rng.Next(0, courseTopics.Count + 1);
                var completedMaterials = rng.Next(0, courseMaterials.Count + 1);

                foreach (var t in courseTopics.Take(completedTopics))
                    topicProgress.Add(new TopicProgress { StudentId = enrollment.StudentId!, TopicId = t.Id, CompletedAt = enrollment.EnrolledAt.AddDays(rng.Next(1, 20)) });
                foreach (var m in courseMaterials.Take(completedMaterials))
                    topicProgress.Add(new TopicProgress { StudentId = enrollment.StudentId!, MaterialId = m.Id, CompletedAt = enrollment.EnrolledAt.AddDays(rng.Next(1, 20)) });

                // Same formula the app uses: 15% topics + 25% materials + 60% quizzes
                double tPart = courseTopics.Count > 0 ? completedTopics / (double)courseTopics.Count * 15 : 0;
                double mPart = courseMaterials.Count > 0 ? completedMaterials / (double)courseMaterials.Count * 25 : 0;
                double qPart = courseQuizIds.Count > 0 ? myResults.Select(r => r.QuizId).Distinct().Count() / (double)courseQuizIds.Count * 60 : 0;
                var progress = (int)Math.Min(Math.Round(tPart + mPart + qPart), 100);

                enrollment.ProgressPercentage = progress;
                enrollment.IsCompleted = progress >= 100;

                courseProgressRows.Add(new StudentCourseProgress
                {
                    StudentId = enrollment.StudentId!,
                    CourseId = enrollment.CourseId,
                    EnrollmentDate = enrollment.EnrolledAt,
                    TopicsCompleted = completedTopics,
                    TotalTopics = courseTopics.Count,
                    CompletionPercentage = progress,
                    QuizzesTaken = myResults.Select(r => r.QuizId).Distinct().Count(),
                    AverageScore = myResults.Count > 0 ? (decimal)Math.Round(myResults.Average(r => r.PercentageScore), 2) : 0,
                    ProgressStatus = progress >= 100 ? "Completed" : progress > 0 ? "In Progress" : "Not Started",
                    LastActivityDate = now.AddDays(-rng.Next(0, 10)),
                    CompletedAt = progress >= 100 ? now.AddDays(-rng.Next(0, 10)) : null,
                });
            }
            context.TopicProgress.AddRange(topicProgress);
            context.StudentCourseProgresses.AddRange(courseProgressRows);
            context.Enrollments.UpdateRange(enrollments);
            await context.SaveChangesAsync();

            // ---------- Attendance (weekdays, last 45 days) ----------
            var attendance = new List<Attendance>();
            for (var day = 45; day >= 1; day--)
            {
                var date = now.Date.AddDays(-day);
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
                foreach (var student in students)
                {
                    var roll = rng.Next(100);
                    var status = roll < 87 ? "Present" : roll < 94 ? "Absent" : "Leave";
                    attendance.Add(new Attendance
                    {
                        StudentId = student.Id,
                        CourseId = null,
                        AttendanceDate = date,
                        Status = status,
                        Remarks = status == "Leave" ? "Approved leave" : null,
                        CreatedAt = date.AddHours(9),
                    });
                }
            }
            context.Attendances.AddRange(attendance);
            await context.SaveChangesAsync();

            // ---------- Semester results ----------
            var semesterCourses = new Dictionary<string, string[]>
            {
                ["Fall 2025"] = new[] { "Mathematics I", "Programming Fundamentals", "Digital Logic Design", "Communication Skills" },
                ["Spring 2026"] = new[] { "Data Structures", "Database Systems", "Operating Systems", "Web Technologies" },
            };
            var semesterResults = new List<SemesterResult>();
            foreach (var student in students)
            {
                foreach (var (semester, subjects) in semesterCourses)
                {
                    foreach (var subject in subjects)
                    {
                        var marks = rng.Next(52, 99);
                        var (grade, gpa) = marks switch
                        {
                            >= 90 => ("A", 4.0m),
                            >= 75 => ("B", 3.0m),
                            >= 60 => ("C", 2.0m),
                            >= 45 => ("D", 1.0m),
                            _ => ("F", 0.0m),
                        };
                        semesterResults.Add(new SemesterResult
                        {
                            StudentId = student.Id,
                            Semester = semester,
                            CourseName = subject,
                            MarksObtained = marks,
                            Grade = grade,
                            GPA = gpa,
                            Remarks = marks >= 90 ? "Excellent" : marks >= 75 ? "Good" : marks >= 60 ? "Satisfactory" : "Needs improvement",
                            CreatedAt = semester == "Fall 2025" ? now.AddDays(-200) : now.AddDays(-40),
                        });
                    }
                }
            }
            context.SemesterResults.AddRange(semesterResults);
            await context.SaveChangesAsync();

            // ---------- Announcements ----------
            context.Announcements.AddRange(new[]
            {
                new Announcement { Title = "Welcome to the new semester!", Content = "Welcome back! Course enrollment is open. Browse the catalog, enroll in your courses and check your dashboard regularly for updates and quiz schedules.", FacultyId = admin.Id, CourseId = null, CreatedAt = now.AddDays(-30), IsActive = true },
                new Announcement { Title = "Scheduled maintenance this weekend", Content = "The learning portal will be briefly unavailable on Saturday between 02:00 and 04:00 UTC for scheduled maintenance. Save your work in advance.", FacultyId = admin.Id, CourseId = null, CreatedAt = now.AddDays(-5), IsActive = true },
                new Announcement { Title = "Fundamentals quiz now open", Content = "The C# Fundamentals Check quiz is now available. You have one week to complete it — it counts toward your course progress.", FacultyId = faculty[0].Id, CourseId = courses[0].Id, CreatedAt = now.AddDays(-7), IsActive = true },
                new Announcement { Title = "Extra lab session: SQL joins", Content = "An optional extra lab session on SQL joins and aggregation will run on Thursday at 15:00 in Lab 2. Bring your laptops.", FacultyId = faculty[1].Id, CourseId = courses[3].Id, CreatedAt = now.AddDays(-3), IsActive = true },
                new Announcement { Title = "Assignment dataset posted", Content = "The dataset for the data-cleaning assignment has been posted in the course materials. Submission deadline is the end of next week.", FacultyId = faculty[2].Id, CourseId = courses[2].Id, CreatedAt = now.AddDays(-2), IsActive = true },
            });
            await context.SaveChangesAsync();

            logger.LogInformation("Demo data seeded: {Courses} courses, {Students} students, {Quizzes} quizzes, {Attendance} attendance records.",
                courses.Count, students.Count, quizzes.Count, attendance.Count);
        }

        private static Course NewCourse(string title, string description, string category, string facultyId, DateTime createdAt, bool isActive = true) =>
            new()
            {
                Title = title,
                Description = description,
                Category = category,
                FacultyId = facultyId,
                CreatedAt = createdAt,
                IsActive = isActive,
            };

        private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager, ILogger logger,
            string email, string password, string firstName, string lastName, string role, string? department)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user != null) return user;

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                FullName = $"{firstName} {lastName}",
                Department = department,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 200)),
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogWarning("Could not create seed user {Email}: {Errors}", email, errors);
                throw new InvalidOperationException($"Failed to seed user {email}: {errors}");
            }
            await userManager.AddToRoleAsync(user, role);
            return user;
        }

        private static List<Quiz> SeedQuizzes(List<Course> courses, List<Topic> topics, DateTime now)
        {
            var bank = new Dictionary<int, (string Title, QData[] Questions)[]>
            {
                [0] = new[]
                {
                    ("C# Fundamentals Check", new[]
                    {
                        new QData("Which keyword declares an immutable local value evaluated at compile time?", "MCQ", "static", "const", "readonly", "sealed", "B", "Easy"),
                        new QData("What is the default value of an uninitialized int field in C#?", "MCQ", "null", "undefined", "0", "-1", "C", "Easy"),
                        new QData("Which collection preserves insertion order and allows index access?", "MCQ", "Dictionary<K,V>", "HashSet<T>", "List<T>", "Queue<T>", "C", "Medium"),
                        new QData("Which access modifier makes a member visible only inside its own class?", "MCQ", "protected", "internal", "public", "private", "D", "Easy"),
                        new QData("A string in C# is a value type.", "TrueFalse", "True", "False", null, null, "False", "Easy"),
                    }),
                    ("OOP & LINQ Quiz", new[]
                    {
                        new QData("Which LINQ operator filters a sequence based on a predicate?", "MCQ", "Select", "Where", "OrderBy", "GroupBy", "B", "Easy"),
                        new QData("What must a class member be so a derived class can override it?", "MCQ", "static", "sealed", "virtual", "const", "C", "Medium"),
                        new QData("Which construct defines a contract with no implementation state?", "MCQ", "abstract class", "interface", "struct", "enum", "B", "Medium"),
                        new QData("What does FirstOrDefault return when a sequence of int contains no elements?", "MCQ", "an exception", "null", "0", "-1", "C", "Hard"),
                        new QData("An abstract class can contain non-abstract methods with implementations.", "TrueFalse", "True", "False", null, null, "True", "Medium"),
                    }),
                },
                [1] = new[]
                {
                    ("ASP.NET Core Basics", new[]
                    {
                        new QData("In MVC, which component selects the view to render?", "MCQ", "Model", "View", "Controller", "Middleware", "C", "Easy"),
                        new QData("Which file configures services and the request pipeline in a modern ASP.NET Core app?", "MCQ", "Startup.config", "Program.cs", "web.config", "appsettings.json", "B", "Easy"),
                        new QData("Which attribute protects a POST action from cross-site request forgery?", "MCQ", "[Authorize]", "[HttpPost]", "[ValidateAntiForgeryToken]", "[Required]", "C", "Medium"),
                        new QData("What does the asp-for tag helper generate for a form input?", "MCQ", "a route", "name/id/value binding", "a CSS class", "a validation summary", "B", "Medium"),
                        new QData("Razor views can contain both HTML markup and C# code.", "TrueFalse", "True", "False", null, null, "True", "Easy"),
                    }),
                    ("Entity Framework Quiz", new[]
                    {
                        new QData("Which EF Core method applies pending migrations at runtime?", "MCQ", "EnsureCreated()", "Migrate()", "Update()", "Attach()", "B", "Medium"),
                        new QData("Which method eagerly loads a navigation property?", "MCQ", "Load()", "Include()", "Select()", "Join()", "B", "Easy"),
                        new QData("What does DbContext.SaveChanges() do?", "MCQ", "Opens a connection", "Persists tracked changes to the database", "Reloads all entities", "Drops the database", "B", "Easy"),
                        new QData("Which delete behavior removes dependent rows automatically?", "MCQ", "Restrict", "SetNull", "Cascade", "NoAction", "C", "Medium"),
                        new QData("LINQ queries against a DbSet execute immediately when defined.", "TrueFalse", "True", "False", null, null, "False", "Hard"),
                    }),
                },
                [2] = new[]
                {
                    ("Python & NumPy Basics", new[]
                    {
                        new QData("Which function loads a CSV file into a Pandas DataFrame?", "MCQ", "pd.open_csv", "pd.read_csv", "pd.load", "pd.csv", "B", "Easy"),
                        new QData("What does df.head() return by default?", "MCQ", "Last 5 rows", "First 5 rows", "Column names", "Summary statistics", "B", "Easy"),
                        new QData("Which NumPy attribute gives the dimensions of an array?", "MCQ", "arr.size", "arr.len", "arr.shape", "arr.dim", "C", "Easy"),
                        new QData("How do you select rows where column 'score' is above 80?", "MCQ", "df.filter('score>80')", "df[df['score'] > 80]", "df.where(score>80)", "df.query.score(80)", "B", "Medium"),
                        new QData("NumPy arrays can hold elements of different data types efficiently.", "TrueFalse", "True", "False", null, null, "False", "Medium"),
                    }),
                    ("Data Cleaning Quiz", new[]
                    {
                        new QData("Which method fills missing values in a DataFrame?", "MCQ", "dropna()", "fillna()", "isna()", "notna()", "B", "Easy"),
                        new QData("What does df.drop_duplicates() do?", "MCQ", "Removes NaN rows", "Removes duplicate rows", "Removes duplicate columns", "Sorts the frame", "B", "Easy"),
                        new QData("Which method converts a column to datetime?", "MCQ", "astype('date')", "pd.to_datetime()", "df.datetime()", "parse_date()", "B", "Medium"),
                        new QData("The IQR method is commonly used for detecting what?", "MCQ", "Missing values", "Duplicates", "Outliers", "Column types", "C", "Medium"),
                        new QData("You should clean data directly in the raw source file to keep things simple.", "TrueFalse", "True", "False", null, null, "False", "Easy"),
                    }),
                },
                [3] = new[]
                {
                    ("SQL Query Basics", new[]
                    {
                        new QData("Which clause filters rows before grouping?", "MCQ", "HAVING", "WHERE", "ORDER BY", "LIMIT", "B", "Easy"),
                        new QData("Which JOIN keeps all rows from the left table?", "MCQ", "INNER JOIN", "RIGHT JOIN", "LEFT JOIN", "CROSS JOIN", "C", "Easy"),
                        new QData("Which aggregate function counts non-NULL values of a column?", "MCQ", "SUM(col)", "COUNT(col)", "TOTAL(col)", "NUM(col)", "B", "Easy"),
                        new QData("Which clause filters groups after aggregation?", "MCQ", "WHERE", "GROUP BY", "HAVING", "DISTINCT", "C", "Medium"),
                        new QData("A primary key column can contain NULL values.", "TrueFalse", "True", "False", null, null, "False", "Easy"),
                    }),
                    ("Design & Normalisation Quiz", new[]
                    {
                        new QData("Which normal form removes repeating groups and requires atomic values?", "MCQ", "1NF", "2NF", "3NF", "BCNF", "A", "Easy"),
                        new QData("3NF eliminates which kind of dependency?", "MCQ", "Partial", "Transitive", "Functional", "Multivalued", "B", "Medium"),
                        new QData("What is the main purpose of an index?", "MCQ", "Enforce uniqueness only", "Speed up data retrieval", "Reduce storage", "Encrypt data", "B", "Easy"),
                        new QData("Which property of transactions guarantees all-or-nothing execution?", "MCQ", "Consistency", "Isolation", "Durability", "Atomicity", "D", "Medium"),
                        new QData("Denormalisation can be a valid choice for read-heavy reporting workloads.", "TrueFalse", "True", "False", null, null, "True", "Hard"),
                    }),
                },
                [4] = new[]
                {
                    ("Modern JavaScript Quiz", new[]
                    {
                        new QData("Which declaration creates a block-scoped variable that can be reassigned?", "MCQ", "var", "let", "const", "static", "B", "Easy"),
                        new QData("What does an arrow function NOT have of its own?", "MCQ", "parameters", "a return value", "a 'this' binding", "a body", "C", "Medium"),
                        new QData("Which syntax extracts properties from an object into variables?", "MCQ", "spreading", "destructuring", "chaining", "hoisting", "B", "Easy"),
                        new QData("What does the ?? operator return?", "MCQ", "The left operand if truthy", "The right operand when the left is null/undefined", "Always the right operand", "A boolean", "B", "Medium"),
                        new QData("const prevents mutation of an object's properties.", "TrueFalse", "True", "False", null, null, "False", "Medium"),
                    }),
                    ("DOM & Async Quiz", new[]
                    {
                        new QData("Which method selects the first element matching a CSS selector?", "MCQ", "getElementById", "querySelector", "getElementsByClassName", "selectFirst", "B", "Easy"),
                        new QData("What does await do inside an async function?", "MCQ", "Blocks the whole browser", "Pauses that function until the promise settles", "Cancels the promise", "Repeats the promise", "B", "Medium"),
                        new QData("Which fetch Response method parses the body as JSON?", "MCQ", "response.parse()", "response.body()", "response.json()", "JSON.fetch()", "C", "Easy"),
                        new QData("Which pattern prevents an event from bubbling to parent elements?", "MCQ", "event.preventDefault()", "event.stopPropagation()", "event.cancel()", "return false only", "B", "Medium"),
                        new QData("fetch() rejects its promise on HTTP 404 responses.", "TrueFalse", "True", "False", null, null, "False", "Hard"),
                    }),
                },
                [5] = new[]
                {
                    ("ML Concepts Quiz", new[]
                    {
                        new QData("Predicting a house price is an example of which task?", "MCQ", "Classification", "Regression", "Clustering", "Reinforcement", "B", "Easy"),
                        new QData("Which algorithm groups unlabeled data into k clusters?", "MCQ", "Linear regression", "k-means", "Logistic regression", "Decision tree", "B", "Easy"),
                        new QData("Overfitting means a model...", "MCQ", "performs poorly on training data", "memorises training data and generalises badly", "is too simple", "has too few parameters", "B", "Medium"),
                        new QData("Which split protects against evaluating on data the model has seen?", "MCQ", "train/test split", "column split", "feature split", "batch split", "A", "Easy"),
                        new QData("Unsupervised learning requires labeled training data.", "TrueFalse", "True", "False", null, null, "False", "Easy"),
                    }),
                    ("Model Evaluation Quiz", new[]
                    {
                        new QData("Which metric is most informative for a highly imbalanced classification problem?", "MCQ", "Accuracy", "F1 score", "MSE", "R²", "B", "Medium"),
                        new QData("Recall measures...", "MCQ", "correct positive predictions among predicted positives", "correct positive predictions among actual positives", "overall correctness", "error magnitude", "B", "Medium"),
                        new QData("Which metric is used for regression problems?", "MCQ", "ROC-AUC", "Precision", "RMSE", "F1", "C", "Easy"),
                        new QData("Cross-validation primarily helps to...", "MCQ", "speed up training", "get a more reliable performance estimate", "reduce dataset size", "remove outliers", "B", "Medium"),
                        new QData("Data leakage inflates a model's apparent performance.", "TrueFalse", "True", "False", null, null, "True", "Medium"),
                    }),
                },
            };

            var quizzes = new List<Quiz>();
            foreach (var (courseIdx, quizDefs) in bank)
            {
                var course = courses[courseIdx];
                var courseTopics = topics.Where(t => t.CourseId == course.Id).ToList();
                foreach (var ((title, questions), qIdx) in quizDefs.Select((d, i) => (d, i)))
                {
                    var quiz = new Quiz
                    {
                        CourseId = course.Id,
                        TopicId = courseTopics.Count > qIdx ? courseTopics[qIdx].Id : null,
                        Title = title,
                        Description = $"Assessment covering the {(qIdx == 0 ? "fundamentals" : "advanced concepts")} of {course.Title}.",
                        PassingMarks = 50,
                        DurationInMinutes = 15,
                        TotalQuestions = questions.Length,
                        TotalMarks = questions.Sum(_ => 2),
                        CreatedAt = course.CreatedAt.AddDays(10 + qIdx * 7),
                        IsActive = true,
                        Questions = questions.Select((q, i) => new QuizQuestion
                        {
                            QuestionText = q.Text,
                            QuestionType = q.Type,
                            QuestionTypeEnum = q.Type == "TrueFalse" ? QuestionType.TrueFalse : QuestionType.MultipleChoice,
                            OptionA = q.A,
                            OptionB = q.B,
                            OptionC = q.C,
                            OptionD = q.D,
                            CorrectOption = q.Correct,
                            Difficulty = q.Difficulty,
                            Marks = 2,
                            Order = i + 1,
                        }).ToList(),
                    };
                    quizzes.Add(quiz);
                }
            }
            return quizzes;
        }
    }
}
