using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using EduConnect.Data;
using EduConnect.Models;
using EduConnect.Services;

var builder = WebApplication.CreateBuilder(args);

// Machine-local secrets (API keys) live outside version control.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// ---------- Data access ----------
// SQLite database lives under App_Data (created on first run). A relative Data Source
// is anchored to the content root — under IIS in-process hosting the process working
// directory is NOT the app folder, so relative paths would otherwise land in system32.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=App_Data/educonnect.db";
var sqliteBuilder = new SqliteConnectionStringBuilder(connectionString);
if (!Path.IsPathRooted(sqliteBuilder.DataSource))
{
    sqliteBuilder.DataSource = Path.Combine(builder.Environment.ContentRootPath, sqliteBuilder.DataSource);
    connectionString = sqliteBuilder.ToString();
}
Directory.CreateDirectory(Path.GetDirectoryName(sqliteBuilder.DataSource)!);
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ---------- Identity ----------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Persist Data Protection keys next to the database. Under IIS the app-pool identity
// has no user profile, so without this every recycle would generate new keys and
// invalidate all sign-in cookies and antiforgery tokens.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")))
    .SetApplicationName("EduConnect");

// ---------- MVC ----------
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

// ---------- Application services ----------
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
if (OperatingSystem.IsWindows())
{
    builder.Services.AddScoped<VideoGenerationService>();
}

// AI provider priority: OmniRoute gateway (when enabled) > Gemini (when a key is
// configured) > deterministic offline mock, so AI features always keep working.
var geminiApiKey = builder.Configuration["AI:GeminiApiKey"];
if (builder.Configuration.GetValue("AI:OmniRoute:Enabled", false))
{
    builder.Services.AddScoped<IAIService, OmniRouteAIService>();
}
else if (!string.IsNullOrWhiteSpace(geminiApiKey))
{
    builder.Services.AddScoped<IAIService>(sp =>
        new GeminiAIService(geminiApiKey, sp.GetRequiredService<ILogger<GeminiAIService>>()));
}
else
{
    builder.Services.AddScoped<IAIService>(sp =>
        new MockAIService(sp.GetRequiredService<ILogger<MockAIService>>()));
}

// Forwarded headers so the app detects the original scheme behind a reverse proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// ---------- Pipeline ----------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");

    // HTTPS enforcement is opt-in so HTTP-only hosts (e.g. IIS on a LAN, or a TLS-
    // terminating proxy) don't redirect into a binding that doesn't exist.
    if (app.Configuration.GetValue("Security:EnforceHttps", false))
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// ---------- Database migration + seeding ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(scope.ServiceProvider, app.Configuration, app.Logger);
}

app.Run();
