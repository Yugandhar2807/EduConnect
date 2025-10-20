# API Routes and Endpoints

## Authentication Routes
```
GET  /Account/Login              - Display login page
POST /Account/Login              - Process login
GET  /Account/Register           - Display registration page
POST /Account/Register           - Create new user account
POST /Account/Logout             - Logout current user
GET  /Account/Lockout            - Display account locked page
```

## Home Routes
```
GET  /                           - Home page / Dashboard redirect
GET  /Home/Index                 - Home page
GET  /Home/Privacy               - Privacy policy page
GET  /Home/Error                 - Error page
```

## Faculty Routes (Requires Faculty or Admin role)
```
GET  /Faculty/Dashboard          - Faculty dashboard with course list
POST /Faculty/CreateCourse       - Create new course
GET  /Faculty/CreateCourse       - Display course creation form
POST /Faculty/EditCourse/{id}    - Update existing course
GET  /Faculty/EditCourse/{id}    - Display edit course form
POST /Faculty/DeleteCourse/{id}  - Delete course
GET  /Faculty/CourseDetails/{id} - View course details
```

## Student Routes (Requires Student role)
```
GET  /Student/Dashboard          - Student dashboard with enrolled courses
GET  /Student/BrowseCourses      - Browse available courses for enrollment
POST /Student/EnrollCourse       - Enroll in a course
GET  /Student/CourseDetails/{id} - View enrolled course details
GET  /Student/MyProgress         - View learning progress and quiz results
```

## Data Models and Structure

### Users
- **LoginViewModel**
  - Email
  - Password
  - RememberMe

- **RegisterViewModel**
  - Email
  - FirstName
  - LastName
  - Password
  - ConfirmPassword
  - Role (Student/Faculty)

- **ApplicationUser**
  - Id, UserName, Email
  - FirstName, LastName
  - CreatedAt, IsActive
  - Relationships: Enrollments, CreatedCourses, QuizResults

### Courses
- **Properties**: Id, Title, Description, Category, Credits, FacultyId, CreatedAt, UpdatedAt, IsActive
- **Relationships**: Faculty, Materials, Enrollments, Quizzes

### Materials
- **Properties**: Id, Title, Description, FilePath, FileType, CourseId, UploadedAt, FileSize
- **Relationships**: Course

### Enrollments
- **Properties**: Id, StudentId, CourseId, EnrolledAt, ProgressPercentage, IsCompleted
- **Relationships**: Student, Course

### Quizzes
- **Properties**: Id, Title, Description, CourseId, TotalQuestions, TotalMarks, PassingMarks, DurationInMinutes, CreatedAt, IsActive
- **Relationships**: Course, Questions, Results

### QuizQuestions
- **Properties**: Id, QuestionText, QuizId, OptionA, OptionB, OptionC, OptionD, CorrectOption, Marks
- **Relationships**: Quiz

### QuizResults
- **Properties**: Id, QuizId, StudentId, MarksObtained, TotalMarks, PercentageScore, IsPassed, AttemptedAt, DurationTakenInSeconds
- **Relationships**: Quiz, Student

### Announcements
- **Properties**: Id, Title, Content, FacultyId, CourseId, CreatedAt, IsActive
- **Relationships**: Faculty, Course

## Form Data Requirements

### Course Creation/Edit
- Title (required)
- Description (optional)
- Category (required)
- Credits (required)
- IsActive (checkbox)

### User Registration
- FirstName (required)
- LastName (required)
- Email (required, unique)
- Password (required, min 6 chars)
- ConfirmPassword (must match Password)
- Role (required: Student or Faculty)

## HTTP Status Codes

- **200 OK** - Successful GET request
- **201 Created** - Resource created successfully
- **204 No Content** - Successful DELETE
- **400 Bad Request** - Invalid input
- **401 Unauthorized** - Authentication required
- **403 Forbidden** - Permission denied
- **404 Not Found** - Resource not found
- **500 Internal Server Error** - Server error

## Authorization Rules

### Anonymous Users
- Can access: Login, Register, Home, Privacy

### Authenticated (All Roles)
- Can access: Dashboard (redirects based on role)
- Can logout

### Faculty/Admin Only
- Full access to: Faculty Dashboard, Course Management
- Can view: Student enrollments, Quiz results

### Student Only
- Full access to: Student Dashboard, Browse Courses, Enrollment
- Can view: Enrolled courses, materials, take quizzes
- Can access: Progress tracking

## Database Relationships

```
ApplicationUser (1) ------ (Many) Enrollment
    |
    +------ (Many) Course (Faculty creates)
    |
    +------ (Many) QuizResult (Student takes)

Course (1) ------ (Many) Enrollment
   |
   +------ (Many) Material
   |
   +------ (Many) Quiz

Quiz (1) ------ (Many) QuizQuestion
  |
  +------ (Many) QuizResult

QuizResult ------ ApplicationUser (Student)
```

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EduConnectDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Connection String Options

**LocalDB:**
```
Server=(localdb)\mssqllocaldb;Database=EduConnectDb;Trusted_Connection=true;MultipleActiveResultSets=true
```

**SQL Server Express (Named Instance):**
```
Server=.\SQLEXPRESS;Database=EduConnectDb;Trusted_Connection=true;MultipleActiveResultSets=true
```

**Azure SQL Database:**
```
Server=tcp:servername.database.windows.net,1433;Initial Catalog=EduConnectDb;Persist Security Info=False;User ID=username;Password=password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

---

For more information, see README.md and SETUP.md
