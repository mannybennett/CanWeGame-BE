// Program.cs
using Microsoft.EntityFrameworkCore;
using CanWeGame.API.Data; // For ApplicationDbContext
using CanWeGame.API.Services; // For PasswordHasher

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 1. Add Controllers for API endpoints
builder.Services.AddControllers();

// 2. Configure SQLite database connection
// Reads the connection string named "DefaultConnection" from appsettings.json
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Register your custom services (e.g., PasswordHasher, though it's static)
// builder.Services.AddScoped<PasswordHasher>(); // Example if not static

// 4. JWT Authentication (TEMPORARILY REMOVED)
// Removed: builder.Services.AddAuthentication(...)
// Removed: builder.Services.AddAuthorization();
// You won't see Jwt:Key, Issuer, Audience in appsettings.json used by code anymore for now.

// 5. Configure CORS (Cross-Origin Resource Sharing)
// This is crucial for your React frontend running on a different port (e.g., 3000)
// to be able to make requests to your backend (e.g., 7001).
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", // Define a policy name
        builder => builder
            .WithOrigins("http://localhost:3000") // Allow requests from your React development server
                                                  // In production, this would be your React app's domain
            .AllowAnyMethod()                     // Allow GET, POST, PUT, DELETE, etc.
            .AllowAnyHeader()                     // Allow any HTTP headers (e.g., Content-Type, Authorization)
            .AllowCredentials());                 // Allow credentials like cookies, authorization headers
});


// 6. Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline (Middleware)

// In Development environment, use Swagger UI for API documentation and testing
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Enables the Swagger middleware
    app.UseSwaggerUI(); // Enables the Swagger UI (web page)
}

app.UseHttpsRedirection(); // Redirects HTTP requests to HTTPS (important for security)

// Use the CORS policy we defined. This must be before UseAuthorization (which is now removed).
app.UseCors("AllowReactApp");

// Authentication and Authorization (TEMPORARILY REMOVED)
// Removed: app.UseAuthentication();
// Removed: app.UseAuthorization();

// Maps incoming requests to controller actions.
app.MapControllers();

// Runs the application. This is a blocking call that starts the web server.
app.Run();
