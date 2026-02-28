# EduConnect - Database Schema & API Endpoints Reference

## Database Tables & SQL Queries

### 1. **AspNetUsers** (ASP.NET Identity - Core User Table)
```sql
CREATE TABLE [AspNetUsers] (
    [Id] NVARCHAR(450) PRIMARY KEY,
    [UserName] NVARCHAR(256),
    [Email] NVARCHAR(256),
    [EmailConfirmed] BIT,
    [PasswordHash] NVARCHAR(MAX),
    [SecurityStamp] NVARCHAR(MAX),
    [ConcurrencyStamp] NVARCHAR(MAX),
    [PhoneNumber] NVARCHAR(MAX),
    [PhoneNumberConfirmed] BIT,
    [TwoFactorEnabled] BIT,
    [LockoutEnd] DATETIMEOFFSET,
    [LockoutEnabled] BIT,
    [AccessFailedCount] INT,
    [FullName] NVARCHAR(MAX),
    [Department] NVARCHAR(MAX),
    [FirstName] NVARCHAR(MAX),
    [LastName] NVARCHAR(MAX)
);

-- Get all users with their roles
SELECT u.Id, u.UserName, u.Email, u.FullName, r.Name as Role
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id;

-- Get only Students
SELECT * FROM AspNetUsers 
WHERE Id IN (SELECT UserId FROM AspNetUserRoles WHERE RoleId = (SELECT Id FROM AspNetRoles WHERE Name = 'Student'));

-- Get only Faculty
SELECT * FROM AspNetUsers 
WHERE Id IN (SELECT UserId FROM AspNetUserRoles WHERE RoleId = (SELECT Id FROM AspNetRoles WHERE Name = 'Faculty'));

-- Get only Admins
SELECT * FROM AspNetUsers 
WHERE Id IN (SELECT UserId FROM AspNetUserRoles WHERE RoleId = (SELECT Id FROM AspNetRoles WHERE Name = 'Admin'));
```

---

### 2. **Courses**
```sql
CREATE TABLE [Courses] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [Title] NVARCHAR(MAX),
    [Description] NVARCHAR(MAX),
    [Category] NVARCHAR(MAX),
    [FacultyId] NVARCHAR(450),
    [CreatedAt] DATETIME2,
    [UpdatedAt] DATETIME2,
    [IsActive] BIT,
    FOREIGN KEY ([FacultyId]) REFERENCES [AspNetUsers]([Id]) ON DELETE RESTRICT
);

-- Get all active courses
SELECT * FROM Courses WHERE IsActive = 1;

-- Get all courses by a specific faculty
SELECT * FROM Courses WHERE FacultyId = 'faculty_user_id' ORDER BY CreatedAt DESC;

-- Get course with all details (topics, materials, quizzes)
SELECT c.*, 
    (SELECT COUNT(*) FROM Topics WHERE CourseId = c.Id) as TopicCount,
    (SELECT COUNT(*) FROM Materials WHERE CourseId = c.Id) as MaterialCount,
    (SELECT COUNT(*) FROM Quizzes WHERE CourseId = c.Id) as QuizCount,
    (SELECT COUNT(*) FROM Enrollments WHERE CourseId = c.Id) as StudentCount
FROM Courses c
WHERE c.Id = 1;

-- Delete a course (cascades to topics, materials, quizzes, enrollments)
DELETE FROM Courses WHERE Id = 1;
```

---

### 3. **Topics**
```sql
CREATE TABLE [Topics] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [CourseId] INT NOT NULL,
    [Title] NVARCHAR(MAX),
    [Description] NVARCHAR(MAX),
    [OrderIndex] INT,
    [CreatedAt] DATETIME2,
    FOREIGN KEY ([CourseId]) REFERENCES [Courses]([Id]) ON DELETE CASCADE
);

-- Get all topics for a course
SELECT * FROM Topics WHERE CourseId = 1 ORDER BY OrderIndex;

-- Get topic with materials and quizzes
SELECT t.*, 
    (SELECT COUNT(*) FROM Materials WHERE TopicId = t.Id) as MaterialCount,
    (SELECT COUNT(*) FROM Quizzes WHERE TopicId = t.Id) as QuizCount
FROM Topics t
WHERE t.Id = 1;
```

---

### 4. **Materials**
```sql
CREATE TABLE [Materials] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [Title] NVARCHAR(MAX),
    [Description] NVARCHAR(MAX),
    [FileType] NVARCHAR(MAX),
    [FilePath] NVARCHAR(MAX),
    [CourseId] INT NOT NULL,
    [TopicId] INT NULL,
    [UploadedAt] DATETIME2,
    FOREIGN KEY ([CourseId]) REFERENCES [Courses]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([TopicId]) REFERENCES [Topics]([Id]) ON DELETE SET NULL
);

-- Get all materials for a course
SELECT * FROM Materials WHERE CourseId = 1 ORDER BY UploadedAt DESC;

-- Get materials by topic
SELECT * FROM Materials WHERE TopicId = 5 ORDER BY UploadedAt DESC;

-- Get materials by file type
SELECT * FROM Materials WHERE CourseId = 1 AND FileType = 'PDF' ORDER BY UploadedAt DESC;
```

---

### 5. **Enrollments**
```sql
CREATE TABLE [Enrollments] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [StudentId] NVARCHAR(450) NOT NULL,
    [CourseId] INT NOT NULL,
    [EnrolledAt] DATETIME2,
    [IsCompleted] BIT,
    [Progress] FLOAT,
    [CompletedAt] DATETIME2,
    FOREIGN KEY ([StudentId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([CourseId]) REFERENCES [Courses]([Id]) ON DELETE CASCADE,
    UNIQUE([StudentId], [CourseId])
);

-- Get enrolled courses for a student
SELECT c.Id, c.Title, c.Description, c.Category, e.EnrolledAt, e.IsCompleted, e.Progress
FROM Enrollments e
JOIN Courses c ON e.CourseId = c.Id
WHERE e.StudentId = 'student_id'
ORDER BY e.EnrolledAt DESC;

-- Get enrolled students in a course
SELECT u.Id, u.UserName, u.Email, e.EnrolledAt, e.Progress, e.IsCompleted
FROM Enrollments e
JOIN AspNetUsers u ON e.StudentId = u.Id
WHERE e.CourseId = 1
ORDER BY e.EnrolledAt;

-- Get enrollment statistics for a course
SELECT 
    CourseId,
    COUNT(*) as TotalEnrollments,
    COUNT(CASE WHEN IsCompleted = 1 THEN 1 END) as CompletedEnrollments,
    AVG(Progress) as AverageProgress
FROM Enrollments
GROUP BY CourseId;
```

---

### 6. **Quizzes**
```sql
CREATE TABLE [Quizzes] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [Title] NVARCHAR(MAX),
    [Description] NVARCHAR(MAX),
    [CourseId] INT NOT NULL,
    [TopicId] INT NULL,
    [DueDate] DATETIME2,
    [CreatedAt] DATETIME2,
    [TimeLimit] INT,
    [PassingScore] FLOAT,
    FOREIGN KEY ([CourseId]) REFERENCES [Courses]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([TopicId]) REFERENCES [Topics]([Id]) ON DELETE SET NULL
);

-- Get all quizzes in a course
SELECT * FROM Quizzes WHERE CourseId = 1 ORDER BY CreatedAt DESC;

-- Get active quizzes (not yet due)
SELECT * FROM Quizzes 
WHERE CourseId = 1 AND DueDate > GETUTCDATE()
ORDER BY DueDate;

-- Get quiz with question count
SELECT q.*, COUNT(qq.Id) as QuestionCount
FROM Quizzes q
LEFT JOIN QuizQuestions qq ON q.Id = qq.QuizId
WHERE q.Id = 1
GROUP BY q.Id, q.Title, q.Description, q.CourseId, q.TopicId, q.DueDate, q.CreatedAt, q.TimeLimit, q.PassingScore;
```

---

### 7. **QuizQuestions**
```sql
CREATE TABLE [QuizQuestions] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [QuizId] INT NOT NULL,
    [QuestionText] NVARCHAR(MAX),
    [QuestionType] INT,
    [Difficulty] INT,
    [Points] FLOAT,
    [OptionA] NVARCHAR(MAX),
    [OptionB] NVARCHAR(MAX),
    [OptionC] NVARCHAR(MAX),
    [OptionD] NVARCHAR(MAX),
    [CorrectAnswer] NVARCHAR(MAX),
    [CreatedAt] DATETIME2,
    FOREIGN KEY ([QuizId]) REFERENCES [Quizzes]([Id]) ON DELETE CASCADE
);

-- Get all questions for a quiz
SELECT * FROM QuizQuestions WHERE QuizId = 1 ORDER BY CreatedAt;

-- Get question statistics for a quiz
SELECT 
    QuizId,
    COUNT(*) as TotalQuestions,
    CAST(AVG(Points) as DECIMAL(5,2)) as AveragePoints,
    COUNT(CASE WHEN Difficulty = 0 THEN 1 END) as EasyCount,
    COUNT(CASE WHEN Difficulty = 1 THEN 1 END) as MediumCount,
    COUNT(CASE WHEN Difficulty = 2 THEN 1 END) as HardCount
FROM QuizQuestions
WHERE QuizId = 1
GROUP BY QuizId;
```

---

### 8. **QuizResults**
```sql
CREATE TABLE [QuizResults] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [QuizId] INT NOT NULL,
    [StudentId] NVARCHAR(450) NOT NULL,
    [Score] FLOAT,
    [TotalPoints] FLOAT,
    [SubmittedAt] DATETIME2,
    [TimeTaken] INT,
    [Passed] BIT,
    FOREIGN KEY ([QuizId]) REFERENCES [Quizzes]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([StudentId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);

-- Get quiz results for a student
SELECT q.Title, qr.Score, qr.TotalPoints, qr.SubmittedAt, qr.Passed
FROM QuizResults qr
JOIN Quizzes q ON qr.QuizId = q.Id
WHERE qr.StudentId = 'student_id'
ORDER BY qr.SubmittedAt DESC;

-- Get quiz statistics for a quiz
SELECT 
    QuizId,
    COUNT(*) as StudentsTaken,
    CAST(AVG(Score) as DECIMAL(5,2)) as AverageScore,
    CAST(AVG(CAST(TotalPoints as FLOAT)) as DECIMAL(5,2)) as AverageTotalPoints,
    COUNT(CASE WHEN Passed = 1 THEN 1 END) as PassedCount,
    CAST(AVG(CAST(TimeTaken as FLOAT)) as DECIMAL(8,2)) as AverageTimeTaken
FROM QuizResults
WHERE QuizId = 1
GROUP BY QuizId;

-- Get student performance across all quizzes in a course
SELECT 
    u.UserName, 
    COUNT(qr.Id) as QuizzesTaken,
    CAST(AVG(qr.Score) as DECIMAL(5,2)) as AverageScore,
    COUNT(CASE WHEN qr.Passed = 1 THEN 1 END) as PassedQuizzes
FROM QuizResults qr
JOIN Quizzes q ON qr.QuizId = q.Id
JOIN AspNetUsers u ON qr.StudentId = u.Id
WHERE q.CourseId = 1
GROUP BY u.Id, u.UserName
ORDER BY AverageScore DESC;
```

---

### 9. **TopicProgress**
```sql
CREATE TABLE [TopicProgress] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [StudentId] NVARCHAR(450) NOT NULL,
    [TopicId] INT NULL,
    [MaterialId] INT NULL,
    [IsCompleted] BIT,
    [ViewedAt] DATETIME2,
    [CompletedAt] DATETIME2,
    FOREIGN KEY ([StudentId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([TopicId]) REFERENCES [Topics]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([MaterialId]) REFERENCES [Materials]([Id]) ON DELETE CASCADE
);

-- Get topic completion progress for a student
SELECT t.Title, 
    COUNT(tp.Id) as MaterialsViewed,
    SUM(CASE WHEN tp.IsCompleted = 1 THEN 1 ELSE 0 END) as MaterialsCompleted
FROM TopicProgress tp
JOIN Topics t ON tp.TopicId = t.Id
WHERE tp.StudentId = 'student_id'
GROUP BY t.Id, t.Title;

-- Get overall progress for a student in a course
SELECT 
    COUNT(DISTINCT tp.TopicId) as TopicsInteracted,
    SUM(CASE WHEN tp.IsCompleted = 1 THEN 1 ELSE 0 END) as TopicsCompleted
FROM TopicProgress tp
JOIN Topics t ON tp.TopicId = t.Id
WHERE tp.StudentId = 'student_id' AND t.CourseId = 1;
```

---

### 10. **Announcements**
```sql
CREATE TABLE [Announcements] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [Title] NVARCHAR(MAX),
    [Content] NVARCHAR(MAX),
    [FacultyId] NVARCHAR(450) NOT NULL,
    [CourseId] INT NULL,
    [CreatedAt] DATETIME2,
    [UpdatedAt] DATETIME2,
    [IsActive] BIT,
    FOREIGN KEY ([FacultyId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([CourseId]) REFERENCES [Courses]([Id]) ON DELETE SET NULL
);

-- Get announcements for a specific course
SELECT * FROM Announcements 
WHERE CourseId = 1 AND IsActive = 1
ORDER BY CreatedAt DESC;

-- Get global announcements (for all students)
SELECT * FROM Announcements 
WHERE CourseId IS NULL AND IsActive = 1
ORDER BY CreatedAt DESC;
```

---

### 11. **Attendance**
```sql
CREATE TABLE [Attendances] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [StudentId] NVARCHAR(450) NOT NULL,
    [CourseId] INT NULL,
    [AttendanceDate] DATETIME2,
    [Status] NVARCHAR(MAX),
    [Remarks] NVARCHAR(MAX),
    FOREIGN KEY ([StudentId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([CourseId]) REFERENCES [Courses]([Id]) ON DELETE CASCADE
);

-- Get attendance for a student
SELECT * FROM Attendances 
WHERE StudentId = 'student_id'
ORDER BY AttendanceDate DESC;

-- Get attendance statistics
SELECT 
    StudentId,
    COUNT(*) as TotalDays,
    COUNT(CASE WHEN Status = 'Present' THEN 1 END) as PresentDays,
    COUNT(CASE WHEN Status = 'Absent' THEN 1 END) as AbsentDays,
    CAST(
        (COUNT(CASE WHEN Status = 'Present' THEN 1 END) * 100.0) / 
        COUNT(*) 
    as DECIMAL(5,2)) as AttendancePercentage
FROM Attendances
WHERE StudentId = 'student_id'
GROUP BY StudentId;

-- Get attendance by course
SELECT 
    CourseId,
    COUNT(DISTINCT StudentId) as TotalStudents,
    COUNT(*) as TotalRecords,
    CAST(
        (COUNT(CASE WHEN Status = 'Present' THEN 1 END) * 100.0) / 
        COUNT(*) 
    as DECIMAL(5,2)) as OverallAttendancePercentage
FROM Attendances
WHERE CourseId = 1
GROUP BY CourseId;
```

---

### 12. **SemesterResults**
```sql
CREATE TABLE [SemesterResults] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [StudentId] NVARCHAR(450) NOT NULL,
    [Semester] NVARCHAR(MAX),
    [GPA] FLOAT,
    [TotalCredits] INT,
    [CreatedAt] DATETIME2,
    [UpdatedAt] DATETIME2,
    FOREIGN KEY ([StudentId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);

-- Get semester results for a student
SELECT * FROM SemesterResults 
WHERE StudentId = 'student_id'
ORDER BY CreatedAt DESC;

-- Get average GPA
SELECT 
    AVG(GPA) as AverageGPA,
    MAX(GPA) as HighestGPA,
    MIN(GPA) as LowestGPA
FROM SemesterResults
WHERE StudentId = 'student_id';
```

---

### 13. **StudentCourseProgress**
```sql
CREATE TABLE [StudentCourseProgresses] (
    [Id] INT PRIMARY KEY IDENTITY(1,1),
    [StudentId] NVARCHAR(450) NOT NULL,
    [CourseId] INT NOT NULL,
    [ProgressPercentage] FLOAT,
    [LastAccessedAt] DATETIME2,
    [CompletedAt] DATETIME2,
    FOREIGN KEY ([StudentId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([CourseId]) REFERENCES [Courses]([Id]) ON DELETE CASCADE
);

-- Get course progress for a student
SELECT c.Title, scp.ProgressPercentage, scp.LastAccessedAt, scp.CompletedAt
FROM StudentCourseProgresses scp
JOIN Courses c ON scp.CourseId = c.Id
WHERE scp.StudentId = 'student_id'
ORDER BY scp.LastAccessedAt DESC;
```

---

## API Endpoints by Controller

### **Account Controller** - Authentication
Base Path: `/Account`

| Method | Endpoint | Authorization | Purpose |
|--------|----------|----------------|---------|
| GET | `/Account/Login` | AllowAnonymous | Display login form |
| POST | `/Account/Login` | AllowAnonymous | Process user login |
| GET | `/Account/Register` | AllowAnonymous | Display registration form |
| POST | `/Account/Register` | AllowAnonymous | Create new user account |
| GET | `/Account/Logout` | Any Authenticated | Log out user |
| POST | `/Account/Logout` | Any Authenticated | Complete logout |
| GET | `/Account/Lockout` | AllowAnonymous | Display account locked message |

---

### **Student Controller** - Student Operations
Base Path: `/Student`
Authorization: `[Authorize(Roles = "Student")]`

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/Student/Dashboard` | Student dashboard with enrolled courses, announcements, attendance, semester results |
| GET | `/Student/BrowseCourses` | Browse available courses to enroll |
| POST | `/Student/EnrollCourse` | Enroll in a course |
| GET | `/Student/CourseDetails/{id}` | View course details (topics, materials, quizzes) |
| GET | `/Student/MyProgress` | View personal learning progress, attendance, GPA |
| GET | `/Student/ViewMyReport` | View Power BI reports (role-based) |
| GET | `/Student/TakeMaterial/{materialId}` | Access course material |
| GET | `/Student/ViewQuiz/{quizId}` | View quiz details |
| POST | `/Student/SubmitQuiz` | Submit quiz answers |
| GET | `/Student/QuizResults/{quizId}` | View quiz results and scores |
| GET | `/Student/CourseAnalytics/{courseId}` | View course progress analytics |

---

### **Faculty Controller** - Faculty Operations
Base Path: `/Faculty`
Authorization: `[Authorize(Roles = "Faculty,Admin")]`

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/Faculty/Dashboard` | Faculty dashboard with course overview |
| GET | `/Faculty/CreateCourse` | Display create course form |
| POST | `/Faculty/CreateCourse` | Create new course |
| GET | `/Faculty/EditCourse/{id}` | Display edit course form |
| POST | `/Faculty/EditCourse/{id}` | Update course details |
| POST | `/Faculty/DeleteCourse/{id}` | Delete a course (cascades to all dependencies) |
| GET | `/Faculty/CourseDetails/{id}` | View detailed course analytics |
| GET | `/Faculty/UploadMaterial/{courseId}` | Display material upload form |
| POST | `/Faculty/UploadMaterial/{courseId}` | Upload course material (PDF, Video, etc.) |
| POST | `/Faculty/DeleteMaterial/{materialId}` | Delete course material |
| GET | `/Faculty/CreateTopic/{courseId}` | Display topic creation form |
| POST | `/Faculty/CreateTopic` | Create course topic |
| POST | `/Faculty/EditTopic/{topicId}` | Update topic |
| POST | `/Faculty/DeleteTopic/{topicId}` | Delete topic |
| GET | `/Faculty/CreateQuiz/{courseId}` | Display quiz creation form |
| POST | `/Faculty/CreateQuiz` | Create quiz |
| POST | `/Faculty/EditQuiz/{quizId}` | Update quiz |
| POST | `/Faculty/DeleteQuiz/{quizId}` | Delete quiz |
| POST | `/Faculty/AddQuestion/{quizId}` | Add question to quiz |
| POST | `/Faculty/EditQuestion/{questionId}` | Edit quiz question |
| POST | `/Faculty/DeleteQuestion/{questionId}` | Delete quiz question |
| GET | `/Faculty/CreateAnnouncement/{courseId}` | Display announcement form |
| POST | `/Faculty/CreateAnnouncement` | Create announcement |
| POST | `/Faculty/EditAnnouncement/{announcementId}` | Update announcement |
| POST | `/Faculty/DeleteAnnouncement/{announcementId}` | Delete announcement |
| GET | `/Faculty/StudentPerformance/{courseId}` | View student performance analytics |
| GET | `/Faculty/GeneratePDF/{topicId}` | Generate PDF for topic |
| GET | `/Faculty/GenerateTopicPDF/{topicId}` | Generate topic material as PDF |

---

### **Admin Controller** - Administration
Base Path: `/Admin`
Authorization: `[Authorize(Roles = "Admin")]`

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/Admin/Dashboard` | Admin dashboard with system statistics |
| GET | `/Admin/Analytics` | Detailed system analytics |
| GET | `/Admin/ManageUsers` | List all users |
| POST | `/Admin/CreateUser` | Create user (admin function) |
| POST | `/Admin/AssignRole` | Assign role to user |
| POST | `/Admin/RemoveRole` | Remove role from user |
| POST | `/Admin/DeleteUser/{userId}` | Delete user account |
| GET | `/Admin/ManageCourses` | List all courses |
| POST | `/Admin/ActivateCourse/{courseId}` | Activate a course |
| POST | `/Admin/DeactivateCourse/{courseId}` | Deactivate a course |
| GET | `/Admin/ManageAnnouncements` | List all announcements |
| POST | `/Admin/CreateAnnouncement` | Create global announcement |
| POST | `/Admin/EditAnnouncement/{announcementId}` | Edit announcement |
| POST | `/Admin/DeleteAnnouncement/{announcementId}` | Delete announcement |
| GET | `/Admin/StudentRecords` | View all student records |
| POST | `/Admin/ImportAttendance` | Import attendance from CSV |
| POST | `/Admin/ImportSemesterResults` | Import semester results from CSV |
| GET | `/Admin/ExportAnalytics` | Export analytics to Excel |
| GET | `/Admin/SystemSettings` | Manage system settings |
| POST | `/Admin/UpdateSettings` | Update system settings |

---

### **Home Controller** - Public Pages
Base Path: `/Home`

| Method | Endpoint | Authorization | Purpose |
|--------|----------|----------------|---------|
| GET | `/Home/Index` | Any | Public homepage |
| GET | `/Home/Privacy` | Any | Privacy policy page |
| GET | `/Home/Error` | Any | Error page |

---

## External API Integrations

### **Google Gemini AI Service**
**File**: [Services/GeminiAIService.cs](Services/GeminiAIService.cs)

```csharp
// API: https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent
// Key: From appsettings.json -> GeminiApiKey
// Methods:
// - GenerateTopicsAsync() - Generate course topics from title/description
// - GenerateMaterialContentAsync() - Generate material content for topics
// - GenerateQuizQuestionsAsync() - AI-generated quiz questions
// - GenerateStudentFeedbackAsync() - Generate personalized feedback
```

**Example Usage in Faculty Controller**:
```csharp
var topics = await _aiService.GenerateTopicsAsync("C# Programming", "Learn C# from basics to advanced");
var materialContent = await _aiService.GenerateMaterialContentAsync("C# Programming", "Variables and Data Types");
```

---

### **Email Service (SMTP)**
**File**: [Services/EmailService.cs](Services/EmailService.cs)

```csharp
// Methods:
// - SendEmailAsync(to, subject, body) - Send single email
// - SendBulkEmailAsync(recipients, subject, body) - Send bulk emails
// - SendEnrollmentConfirmationAsync() - Course enrollment email
// - SendGradeNotificationAsync() - Grade notification email
// - SendAnnouncementAsync() - Announcement email to students
```

---

### **Power BI Service**
**File**: [Services/PowerBIService.cs](Services/PowerBIService.cs)

```csharp
// Generates embedded Power BI reports for students and faculty
// Role-based URLs configured per student
// Usage: StudentController.ViewMyReport()
```

---

## Database Connection String
**File**: `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EduConnectDb;Trusted_Connection=true;"
  }
}
```

---

## Key Relationships & Cascading

```
ApplicationUser (AspNetUsers)
├── CreatedCourses (Faculty creates courses - ON DELETE RESTRICT)
├── Enrollments (Students enroll - ON DELETE CASCADE)
├── QuizResults (Student quiz submissions - ON DELETE CASCADE)
├── TopicProgress (Student progress tracking - ON DELETE CASCADE)
├── Announcements (Faculty creates - ON DELETE CASCADE)
└── Attendance (Student attendance - ON DELETE CASCADE)

Course
├── Topics (ON DELETE CASCADE)
├── Materials (ON DELETE CASCADE)
├── Quizzes (ON DELETE CASCADE)
├── Enrollments (ON DELETE CASCADE)
└── Announcements (ON DELETE SET NULL)

Topic
├── Materials (ON DELETE SET NULL)
├── Quizzes (ON DELETE SET NULL)
└── TopicProgress (ON DELETE CASCADE)

Quiz
├── QuizQuestions (ON DELETE CASCADE)
└── QuizResults (ON DELETE CASCADE)
```

---

## Default Admin Credentials
```
Email: admin@educonnect.com
Password: Admin@123456
```

---

## Running Database Migrations
```powershell
# Update to latest migration
dotnet ef database update

# Create new migration after model changes
dotnet ef migrations add DescriptiveMigrationName

# View pending migrations
dotnet ef migrations list
```

---

## Important Notes
1. All timestamps use UTC (`DateTime.UtcNow`)
2. Cascading deletes are enabled where appropriate
3. Authorization is enforced at controller/action level with `[Authorize(Roles = "...")]`
4. All async operations use `async Task<T>` pattern
5. Email notifications are sent asynchronously via `IEmailService`
6. AI features are optional and swappable (MockAIService, NullAIService available)
