# EduConnect Setup Guide

## Quick Start

### 1. Prerequisites
- .NET 8.0 SDK or later
- SQL Server (LocalDB included with Visual Studio, or full SQL Server)
- Visual Studio 2022 / VS Code

### 2. Database Setup

**Option A: Using LocalDB (Recommended for Development)**
```powershell
cd C:\Users\Administrator\Downloads\EduNet
dotnet ef database update
```

**Option B: Using Full SQL Server**
Edit `appsettings.json`:
```json
"ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=EduConnectDb;Trusted_Connection=true;MultipleActiveResultSets=true"
}
```

Then run:
```powershell
dotnet ef database update
```

### 3. Install Dependencies
```powershell
dotnet restore
```

### 4. Run Application
```powershell
dotnet run
```

Access at: `https://localhost:5001`

## Default Login

- **Email**: admin@educonnect.com
- **Password**: Admin@123456

After login, you can create new faculty or student accounts using the Register page.

## User Roles

### Admin
- Full system access
- User management
- System configuration

### Faculty
- Create and manage courses
- Upload study materials
- Create quizzes
- Post announcements
- View student performance

### Student
- Browse and enroll in courses
- Access materials
- Attempt quizzes
- Track progress
- View announcements

## Project Structure

```
EduConnect/
├── Models/                    # Data models
│   ├── ApplicationUser.cs
│   ├── Course.cs
│   ├── Enrollment.cs
│   ├── Material.cs
│   ├── Quiz.cs
│   ├── QuizQuestion.cs
│   ├── QuizResult.cs
│   ├── Announcement.cs
│   └── AccountViewModels.cs
├── Controllers/               # MVC Controllers
│   ├── HomeController.cs
│   ├── AccountController.cs
│   ├── FacultyController.cs
│   └── StudentController.cs
├── Views/
│   ├── Shared/               # Shared templates
│   ├── Faculty/              # Faculty views
│   ├── Student/              # Student views
│   └── Account/              # Authentication views
├── Data/
│   └── ApplicationDbContext.cs
├── wwwroot/
│   ├── css/site.css
│   ├── js/site.js
│   └── uploads/              # User uploads
├── Program.cs                # App configuration
├── appsettings.json
└── EduConnect.csproj

```

## Key Features

### Implemented
✅ User authentication and authorization
✅ Role-based access control (Admin, Faculty, Student)
✅ Course creation and management
✅ Student enrollment
✅ Dashboard with statistics
✅ Course details and materials view
✅ Quiz management structure
✅ Progress tracking
✅ Bootstrap responsive UI

### Ready for Development
- File upload for materials
- Quiz attempt functionality
- Advanced progress calculations
- Email notifications
- Real-time features (SignalR)

## Development Commands

### Create a Migration
```powershell
dotnet ef migrations add MigrationName
```

### Apply Migrations
```powershell
dotnet ef database update
```

### Remove Last Migration
```powershell
dotnet ef migrations remove
```

### Build Project
```powershell
dotnet build
```

### Run Tests
```powershell
dotnet test
```

## Database Models

### Users
- ApplicationUser (Extended IdentityUser)
- Roles: Admin, Faculty, Student

### Courses
- Created by Faculty
- Linked to Materials and Quizzes
- Student enrollments

### Materials
- Study resources
- File path, type, and size
- Linked to Courses

### Enrollments
- Student-Course relationships
- Progress tracking
- Completion status

### Quizzes
- Course quizzes
- Questions with options
- Results and scores

### Announcements
- Faculty-created announcements
- Course-specific or general

## Security Notes

⚠️ **Before Production Deployment:**
1. Change default admin credentials
2. Update connection string for production database
3. Enable HTTPS
4. Configure proper CORS policies
5. Implement rate limiting
6. Add SQL Server encryption
7. Enable audit logging

## Troubleshooting

### Database Connection Failed
- Verify SQL Server is running
- Check connection string in `appsettings.json`
- Try using `(localdb)\mssqllocaldb` for LocalDB

### Port 5001 Already in Use
- Edit `Properties/launchSettings.json`
- Change port numbers in both `http` and `https` profiles

### Migration Issues
```powershell
dotnet ef database update --verbose
```

### Package Restore Failed
```powershell
dotnet clean
dotnet restore
```

## Deployment

### Azure App Service
1. Create App Service and SQL Database on Azure
2. Update connection string in Azure Portal
3. Publish using Visual Studio or Azure CLI

### Docker
Create `Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EduConnect.csproj", ""]
RUN dotnet restore "EduConnect.csproj"
COPY . .
RUN dotnet build "EduConnect.csproj" -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/build .
EXPOSE 5001
ENTRYPOINT ["dotnet", "EduConnect.dll"]
```

## Support

For issues or questions:
1. Check README.md for feature overview
2. Review .github/copilot-instructions.md for development guidelines
3. Check database schema in Data/ApplicationDbContext.cs

---

**Version**: 1.0.0  
**Last Updated**: October 2024
