using Microsoft.EntityFrameworkCore;
using DocManagerAI.Data;
using DocManagerAI.Models;
using DocManagerAI.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Railway / production: bind to the PORT env variable ──────────────────────
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddRazorPages();
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
});

// ── SQLite: in production write the DB to /tmp so it persists between restarts
var dbPath = builder.Environment.IsProduction()
    ? "/tmp/app.db"
    : "app.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapRazorPages();

// ── Seed database on first run ────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Users.Any())
    {
        db.Users.AddRange(
            new User { Username = "admin",    PasswordHash = AuthService.Hash("Admin@1234"),   Role = "Admin",    Email = "admin@docmanager.co.za"    },
            new User { Username = "reviewer", PasswordHash = AuthService.Hash("Review@1234"),  Role = "Reviewer", Email = "reviewer@docmanager.co.za" },
            new User { Username = "manager",  PasswordHash = AuthService.Hash("Manage@1234"),  Role = "Manager",  Email = "manager@docmanager.co.za"  },
            new User { Username = "finance",  PasswordHash = AuthService.Hash("Finance@1234"), Role = "Finance",  Email = "finance@docmanager.co.za"  },
            new User { Username = "viewer",   PasswordHash = AuthService.Hash("View@1234"),    Role = "Viewer",   Email = "viewer@docmanager.co.za"   }
        );
        db.SaveChanges();
    }
}

app.Run();
