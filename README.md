# EduConnect - Interactive Learning Portal

A comprehensive web application built with ASP.NET Core MVC and SQL Server for managing interactive learning between students and faculty.

## Features

### Faculty Module
- Create and manage courses
- Upload study materials (PDFs, videos, documents)
- Create and manage quizzes
- Post announcements
- Monitor student performance and progress
- View enrolled students

### Student Module
- Browse and enroll in available courses
- Access study materials
- Attempt quizzes
- Track learning progress
- View performance analytics
- Receive announcements from faculty

## Technology Stack

- **Backend**: ASP.NET Core 8.0 MVC
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: ASP.NET Identity with role-based access
- **Frontend**: HTML, CSS, Bootstrap 5, JavaScript
- **Hosting**: Microsoft Azure ready

## Project Structure

```
EduConnect/
├── Models/                 # Data models (Course, Enrollment, Quiz, etc.)
├── Controllers/            # MVC Controllers
├── Views/                  # Razor views for UI
│   ├── Shared/            # Shared layouts and partials
│   ├── Faculty/           # Faculty module views
│   ├── Student/           # Student module views
│   └── Account/           # Authentication views
├── Data/                  # Database context and migrations
├── Services/              # Business logic services
├── wwwroot/               # Static files (CSS, JS, uploads)
└── Program.cs             # Application startup configuration
```

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- SQL Server (LocalDB or full edition)
- Visual Studio 2022 or VS Code

### Installation

1. **Clone or extract the project**
   ```bash
   cd EduConnect
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

3. **Update the database connection string** in `appsettings.json`:
   ```json
   "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=EduConnectDb;Trusted_Connection=true;"
   }
   ```

4. **Apply database migrations**
   ```bash
   dotnet ef database update
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access the application** at `https://localhost:5001`

## Default Credentials

An admin account is automatically created during the first run:
- **Email**: admin@educonnect.com
- **Password**: Admin@123456

**Note**: Change these credentials in production!

## User Roles

- **Admin**: Full access to all features
- **Faculty**: Can create courses, upload materials, create quizzes, and monitor students
- **Student**: Can enroll in courses, access materials, take quizzes, and track progress

## Database Schema

### Tables
- **Users**: Stores user information with role assignment
- **Courses**: Course information created by faculty
- **Materials**: Study materials linked to courses
- **Enrollments**: Student course registrations
- **Quizzes**: Quiz information and questions
- **QuizResults**: Student quiz attempts and scores
- **Announcements**: Faculty announcements

## Configuration

### Connection String
Update `appsettings.json` for your database:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EduConnectDb;Trusted_Connection=true;"
  }
}
```

### Logging
Configure logging levels in `appsettings.json` and `appsettings.Development.json`

## Deployment

### Azure Deployment
1. Create an Azure App Service and SQL Database
2. Update connection strings in Azure Portal
3. Deploy using Visual Studio or Azure CLI:
   ```bash
   az webapp deployment source config-zip --resource-group <group> --name <app> --src <zip-file>
   ```

## Security Considerations

- Use HTTPS in production
- Change default admin credentials
- Implement rate limiting for sensitive operations
- Use secure password policies
- Enable SQL Server encryption
- Regular backups of the database

## Future Enhancements

- Video streaming integration
- Real-time notifications using SignalR
- Discussion forums per course
- Advanced analytics and reporting
- Mobile application (iOS/Android)
- Integration with third-party services
- Certificate generation for completed courses

## Troubleshooting

### Database Connection Issues
- Ensure SQL Server is running
- Verify connection string in `appsettings.json`
- Check firewall settings

### Migration Errors
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Port Already in Use
Change the port in `launchSettings.json` under Properties folder

## Contributing

Contributions are welcome! Please follow the project structure and coding standards.

## Support

For issues or questions, please refer to the documentation or contact the development team.

## License

This project is licensed under the MIT License - see LICENSE file for details.

---

**Version**: 1.0.0  
**Last Updated**: October 2024  
**Status**: Production Ready
