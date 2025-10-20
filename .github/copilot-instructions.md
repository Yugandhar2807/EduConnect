# EduConnect Development Instructions

## Project Overview
EduConnect is an ASP.NET Core MVC-based interactive learning portal that connects students and faculty within a single system. Faculty can create courses, upload materials, and conduct quizzes, while students can enroll, access materials, and track progress.

## Tech Stack
- ASP.NET Core 8.0 MVC
- SQL Server Database
- Entity Framework Core
- ASP.NET Identity
- Bootstrap 5 Frontend
- Microsoft Azure Ready

## Project Structure
```
EduConnect/
├── Models/                 # Database models
├── Controllers/            # Application controllers
├── Views/                  # Razor view templates
├── Data/                   # DbContext and migrations
├── Services/               # Business logic
├── wwwroot/                # Static assets
└── Program.cs              # Startup configuration
```

## Getting Started

### Setup Database
1. Update `appsettings.json` with your SQL Server connection string
2. Run migrations: `dotnet ef database update`
3. Default admin account created automatically

### Run Application
```bash
dotnet restore
dotnet run
```

Access at: https://localhost:5001

## Default Credentials
- Email: admin@educonnect.com
- Password: Admin@123456

## Key Features Implemented

### Faculty Module
- Dashboard with statistics
- Create, edit, delete courses
- Course management view
- Integration with materials and quizzes

### Student Module
- Dashboard with progress tracking
- Browse available courses
- Course enrollment
- Course details with materials and quizzes
- Progress tracking dashboard

### Authentication
- Role-based access (Admin, Faculty, Student)
- ASP.NET Identity integration
- Secure login/logout

## Database Models
- **ApplicationUser**: Extended Identity user
- **Course**: Course information by faculty
- **Material**: Study materials for courses
- **Enrollment**: Student course registrations
- **Quiz**: Course quizzes
- **QuizQuestion**: Individual quiz questions
- **QuizResult**: Student quiz performance
- **Announcement**: Faculty announcements

## Next Steps for Development

1. **Implement Material Management**
   - File upload functionality
   - Support PDF, Video, Document types
   - Download/streaming capability

2. **Quiz System Enhancement**
   - Quiz question management interface
   - Quiz attempt functionality
   - Score calculation and results

3. **Announcement System**
   - Create announcement interface
   - Distribution logic
   - Dashboard display

4. **Progress Tracking**
   - Calculate progress percentage
   - Update based on material views and quizzes

5. **Advanced Features**
   - Email notifications
   - Real-time notifications (SignalR)
   - Export/reporting
   - Certificate generation

## Important Files
- `Program.cs`: Startup and dependency injection
- `Data/ApplicationDbContext.cs`: Database relationships
- `Models/*`: Data model definitions
- `Views/Shared/_Layout.cshtml`: Master layout

## Development Workflow
1. Create migrations for schema changes: `dotnet ef migrations add MigrationName`
2. Apply migrations: `dotnet ef database update`
3. Test locally before deployment
4. Follow naming conventions for files and classes
5. Keep views organized by controller/feature

## Deployment
- Azure App Service compatible
- Requires SQL Server database
- HTTPS recommended for production
- Change default admin credentials before deployment
