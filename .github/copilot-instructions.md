# EduConnect Development Instructions

## Architecture Overview

EduConnect is a multi-tenant ASP.NET Core 8.0 MVC learning management system with three role-based workflows (Admin, Faculty, Student). The system uses Entity Framework Core with SQL Server and extends ASP.NET Identity for user management.

**Key architectural principle**: Each controller maps to a user role. Cross-cutting concerns (email, AI, PDF generation) live in `Services/` and are injected via dependency injection.

## Critical Models & Relationships

The domain model uses cascading deletes strategically:

- **ApplicationUser** → Course (Faculty creates via `HasMany(CreatedCourses)`, `OnDelete(Restrict)` prevents deletion of courses)
- **Course** → Enrollment, Material, Topic, Quiz (all `OnDelete(Cascade)` - deleting a course cleans up dependencies)
- **Enrollment** → Student + Course (double cascade for cleanup)
- **Material/Quiz** → Topic (optional, `OnDelete(SetNull)`)
- **Topic** → Material, Quiz, TopicProgress (cascade delete)

See [Data/ApplicationDbContext.cs](Data/ApplicationDbContext.cs#L25-L75) for relationship configuration. This structure supports courses structured into topics with nested materials and quizzes.

## Service Integration Patterns

**Email Notifications** ([Services/EmailService.cs](Services/EmailService.cs)):
- Implements async SMTP or HTTP-based service (see `SendEmailAsync`, `SendBulkEmailAsync`)
- Domain-specific helpers: `SendEnrollmentConfirmationAsync`, `SendGradeNotificationAsync`, `SendAnnouncementAsync`
- All methods are async `Task<bool>` with try-catch logging
- Injected in controllers as `private readonly IEmailService _emailService;`

**AI Services** ([Services/IAIService.cs](Services/IAIService.cs) interface):
- Swappable implementations: `GeminiAIService`, `MockAIService`, `NullAIService`
- Configured in `Program.cs` to use Gemini by default (can swap for testing)
- Used for student assistance features

**PDF Generation** ([Services/PdfGenerationService.cs](Services/PdfGenerationService.cs)):
- Generates HTML, then converts via external tool or library
- `GenerateTopicPdfAsync()` method writes formatted HTML with embedded styles
- Returns file path for download

## Development Workflows

### Running the Application
```bash
dotnet restore
dotnet ef database update      # Applies all migrations
dotnet run
```
Access: https://localhost:5001

### Database Changes
1. Modify model in `Models/`
2. `dotnet ef migrations add DescriptiveName`
3. Review generated migration in `Migrations/`
4. `dotnet ef database update`

**Naming pattern**: Migrations in this codebase use timestamps (e.g., `20251229182422_RemoveCreditsFromCourse.cs`). Always cascade delete unless you have a specific reason to restrict.

### Default Credentials
- Admin: admin@educonnect.com / Admin@123456
- Use [AccountController.cs](Controllers/AccountController.cs) for auth flows

## Controller Conventions

- **AccountController**: Register, login, logout (accessible to all)
- **AdminController**: System management, user roles (Admin only - add `[Authorize(Roles = "Admin")]`)
- **FacultyController**: Create/edit courses, materials, quizzes (Faculty only)
- **StudentController**: Browse, enroll, take quizzes, view progress (Student only)
- **HomeController**: Public pages, redirects authenticated users based on role

Protect controller actions with `[Authorize(Roles = "Faculty")]` or `[Authorize(Roles = "Student")]`.

## Configuration & Secrets

- **Connection String**: `appsettings.json` property `DefaultConnection`
- **Email Config**: Usually environment variables or secrets (see `EmailService` constructor)
- **AI API Keys**: Injected via `IConfiguration`, typically from `appsettings.json` or Azure Key Vault
- **Environment-specific**: `appsettings.Development.json`, `appsettings.Production.json` override base settings

## External Dependencies

- **Internal**: Google Gemini AI (optional, swappable), SMTP for email notifications
- **NuGet**: Standard ASP.NET Core, EntityFrameworkCore packages (see `.csproj`)
- **Render/Azure**: Deployment ready (see `Dockerfile`, `Procfile`, `render.yaml`)

## Common Patterns to Follow

1. **Async/Await**: All service methods use `async Task<T>`. Example:
   ```csharp
   public async Task<QuizResult> SubmitQuizAsync(int quizId, List<Answer> answers)
   {
       // validate, calculate score, save to DB, send email async
       await _emailService.SendGradeNotificationAsync(...);
   }
   ```

2. **Dependency Injection**: Constructor inject interfaces, never `new` services:
   ```csharp
   public FacultyController(ApplicationDbContext db, IEmailService emailService) { ... }
   ```

3. **Role-Based Authorization**: Use `[Authorize(Roles = "Faculty")]` on sensitive actions.

4. **View Naming**: Match controller action name. E.g., `FacultyController.CreateCourse()` → `Views/Faculty/CreateCourse.cshtml`

## Essential Files to Understand First

- [Program.cs](Program.cs) — Dependency injection setup, authentication config
- [Data/ApplicationDbContext.cs](Data/ApplicationDbContext.cs) — Relationship definitions
- [Models/ApplicationUser.cs](Models/ApplicationUser.cs) — User role properties
- [Controllers/StudentController.cs](Controllers/StudentController.cs) — Example workflow (browse → enroll → take quiz)
