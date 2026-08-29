# EduConnect — Interactive Learning Portal

A complete learning-management web application built with **ASP.NET Core 9 MVC**, **Entity Framework Core (SQLite)** and **ASP.NET Identity**. Three fully-featured role-based experiences — Admin, Faculty and Student — with a modern, responsive UI.

## Quick Start

```bash
dotnet run
```

Open **http://localhost:8000**. On first run the app creates `App_Data/educonnect.db`, applies all migrations and seeds a full demo dataset (courses, topics, materials, quizzes, enrollments, results, attendance, announcements).

### Demo Accounts

| Role    | Email                                  | Password      |
|---------|----------------------------------------|---------------|
| Admin   | `admin@educonnect.com`                 | `Admin@123`   |
| Faculty | `sarah.mitchell@educonnect.com`        | `Faculty@123` |
| Faculty | `james.carter@educonnect.com`          | `Faculty@123` |
| Faculty | `priya.sharma@educonnect.com`          | `Faculty@123` |
| Student | `alex.johnson@student.educonnect.com`  | `Student@123` |
| Student | `maria.garcia@student.educonnect.com`  | `Student@123` |

*(8 demo students total — all use `Student@123`.)*

All seed credentials come from the `Seed` section of `appsettings.json` / environment variables (`Seed__AdminEmail`, `Seed__AdminPassword`, …) — nothing is hardcoded. Set `Seed__SeedDemoData=false` to skip demo content (default in Production).

## Features

### Admin
- Dashboard with institution-wide KPIs, enrollment trend, top courses and live activity feed
- Analytics: course performance, faculty workload, grade distribution, attendance trends
- Student & faculty management (create, edit, activate/deactivate, delete) with search/sort/pagination
- Semester results management (record, filter, edit official grades and GPA)
- Excel export of student data

### Faculty
- Teaching dashboard with per-course stats and recent quiz attempts
- Course management: topics, study materials (file upload or text notes), quizzes
- Quiz builder with **multiple choice, true/false and coding questions**, plus AI-assisted question generation
- Per-course student monitoring (progress, quiz averages, last activity)
- Announcements (per-course or portal-wide)
- Daily attendance marking (any date) and searchable attendance log

### Student
- Personal dashboard: courses, progress, announcements, recent results, attendance
- Course catalog with search and category filters, one-click enrollment
- Course workspace: topics and materials with completion tracking (15% topics + 25% materials + 60% quizzes = course progress)
- Timed quiz taking with instant scoring; coding questions run real Python/JavaScript
- Quiz history, progress reports, attendance history and a chart-based analytics page

### Platform
- Role-based access control (ASP.NET Identity, lockout after 5 failed sign-ins)
- Responsive sidebar app shell (mobile/tablet/desktop), toasts, confirmation dialogs
- Client + server validation on every form; antiforgery protection on every mutation
- Email notifications (SendGrid via Twilio) — logs to console in development when unconfigured
- Optional Google Gemini integration for AI content generation (offline mock used when no API key is set)

## Configuration

| Setting | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQLite path (default `App_Data/educonnect.db`; `/var/data/educonnect.db` in Production) |
| `Seed:AdminEmail` / `Seed:AdminPassword` | Bootstrap admin account |
| `Seed:SeedDemoData` | Seed demo catalog when the database is empty |
| `Twilio:AccountSid` / `Twilio:AuthToken` | SendGrid email credentials (optional) |
| `AI:GeminiApiKey` | Google Gemini API key (optional — mock AI used otherwise) |

## Project Structure

```
EduConnect/
├── Controllers/        # Account, Admin, Faculty, Student, Home
├── Models/             # Entities + view models (validated form models, dashboards)
├── Data/               # ApplicationDbContext, DbSeeder (demo data)
├── Migrations/         # EF Core migrations
├── Services/           # Email, Excel export, AI (Gemini + offline mock), PDF
├── Views/              # Razor views (sidebar app shell in Shared/_Layout.cshtml)
├── wwwroot/
│   ├── css/app.css     # Design system
│   └── js/app.js       # Toasts, confirm dialogs, data tables, antiforgery helper
└── Program.cs          # Startup, DI, pipeline, migration + seeding
```

## Deployment

### IIS (Windows — recommended for a laptop/on-prem "always on" install)

```powershell
dotnet publish EduConnect.csproj -c Release -o publish
# then, in an ELEVATED PowerShell:
powershell -ExecutionPolicy Bypass -File .\Setup-IIS.ps1
```

[Setup-IIS.ps1](Setup-IIS.ps1) is idempotent: it enables the required IIS features, installs the
.NET 9 Hosting Bundle if missing, deploys to `C:\inetpub\EduConnect`, creates the `EduConnect`
app pool (No Managed Code, AlwaysRunning) and site on port 80, sets least-privilege permissions,
opens the firewall for LAN access, and warms the site up. Re-running it **redeploys the app
without touching the database or uploaded files**. The site starts automatically with Windows.

To redeploy after a code change: `dotnet publish -c Release -o publish`, then re-run the script.
To reset demo data: stop the `EduConnect` app pool in IIS Manager, delete
`C:\inetpub\EduConnect\App_Data\educonnect.db`, start the pool again.

### Docker / Render

A `Dockerfile` and `render.yaml` are included for container deployment (SQLite on a persistent
disk at `/var/data`, configured via env vars in `render.yaml`). For any production install:

1. Set `Seed__AdminPassword` to a strong secret.
2. Provide `Twilio__*` credentials if real email is needed.
3. `ASPNETCORE_ENVIRONMENT=Production` disables demo seeding; set `Security__EnforceHttps=true`
   when the host terminates TLS.

## Notes

- The coding-question runner executes short snippets with the server's local Python/Node when available; use an isolated sandbox for untrusted production workloads.
- To reset the demo, stop the app and delete `App_Data/educonnect.db` — it re-seeds on the next start.
