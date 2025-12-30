using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EduConnect.Data;
using EduConnect.Models;
using EduConnect.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Configure forwarded headers (X-Forwarded-For, X-Forwarded-Proto) so the app
// correctly detects the original request scheme when behind a proxy (e.g., Render)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Register Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// Register PDF Generation Service
builder.Services.AddScoped<PdfGenerationService>();

// Register AI Service
var geminiApiKey = builder.Configuration["AI:GeminiApiKey"];
// Use Mock AI Service for demonstration (Gemini API free tier quota exhausted)
// Change this to GeminiAIService when you add billing to your Google Cloud project
builder.Services.AddScoped<IAIService>(sp =>
    new MockAIService(sp.GetRequiredService<ILogger<MockAIService>>())
);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
// Process forwarded headers before other middleware so `Request.Scheme` is correct
app.UseForwardedHeaders();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Apply migrations and initialize database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Initialize database with default roles and admin user
await InitializeDatabase(app);

app.Run();

// Database initialization method
async Task InitializeDatabase(WebApplication webApp)
{
    using (var scope = webApp.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        try
        {
            // Create roles
            string[] roles = { "Admin", "Faculty", "Student" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!result.Succeeded)
                    {
                        webApp.Logger.LogWarning($"Failed to create role {role}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }

            // Create default admin user
            var adminUser = await userManager.FindByEmailAsync("admin@educonnect.com");
            if (adminUser == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@educonnect.com",
                    Email = "admin@educonnect.com",
                    FirstName = "Admin",
                    LastName = "User",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(admin, "Admin@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                    webApp.Logger.LogInformation("Admin user created successfully.");
                }
                else
                {
                    webApp.Logger.LogWarning($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }

            // Create authorized student users
            var studentEmails = new[]
            {
                ("22X01A6748@nrcmec.org", "Chichu@2004", "Yugandhar", "Reddy"),
                ("22X01A6647@nrcmec.org", "Chichu@2005", "Sindhu", "Kumar"),
                ("22X01A6761@nrcmec.org", "Chichu@2003", "Vikas", "Singh"),
                ("22X01A6751@nrcmec.org", "Chichu@2002", "Pankaj", "Gupta"),
                ("22X01A6762@nrcmec.org", "Chichu@2001", "Sujana", "Devi")
            };

            foreach (var (email, password, firstName, lastName) in studentEmails)
            {
                var studentUser = await userManager.FindByEmailAsync(email);
                if (studentUser == null)
                {
                    var student = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(student, password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(student, "Student");
                        webApp.Logger.LogInformation($"Student user {email} created successfully.");
                    }
                    else
                    {
                        webApp.Logger.LogWarning($"Failed to create student user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }

            // Create authorized faculty user
            var facultyUser = await userManager.FindByEmailAsync("RamuGandikota@gmail.com");
            if (facultyUser == null)
            {
                var faculty = new ApplicationUser
                {
                    UserName = "RamuGandikota@gmail.com",
                    Email = "RamuGandikota@gmail.com",
                    FirstName = "Ramu",
                    LastName = "Gandikota",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(faculty, "Ramu@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(faculty, "Faculty");
                    webApp.Logger.LogInformation("Faculty user created successfully.");
                }
                else
                {
                    webApp.Logger.LogWarning($"Failed to create faculty user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
        catch (Exception ex)
        {
            webApp.Logger.LogError(ex, "An error occurred seeding the database.");
        }
    }
}
