using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// EF Core - SQL Server, Code-First. Update the connection string in
// appsettings.json to point at your own SQL Server instance.
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session - stores the logged-in user's Id and Role between requests,
// so authentication does not rely on a separate token scheme.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Swagger - every endpoint is visible and testable through the browser UI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RESTful API for the RaceDay running-events system. " +
                       "Register at /api/auth/register, then log in at /api/auth/login before " +
                       "trying role-restricted endpoints - Swagger will carry the session cookie automatically."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Swagger is always on (not just in Development) so a marker can open it
// straight away without changing any settings.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseSession();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes the top-level Program class so the test project's
// WebApplicationFactory<Program> can find and start this app.
public partial class Program { }
