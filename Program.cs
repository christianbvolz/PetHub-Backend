using Microsoft.EntityFrameworkCore;
using pethub.Data;
using pethub.Hubs;
using pethub.Services;

var builder = WebApplication.CreateBuilder(args);

// ==================================================================
// 1. CONFIGURATION LOADING (Environment Variables vs Local)
// ==================================================================

// Database Connection String
// Tries to get from Docker/Render env var first, then falls back to local JSON
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

// Frontend URL for CORS (Comma or Semicolon separated)
// Example: "http://localhost:3000;https://pethub.vercel.app"
var frontendUrl =
    Environment.GetEnvironmentVariable("FRONTEND_URL")
    ?? "http://localhost:3000;http://localhost:5173";

// JWT Secret (For future Authentication)
var jwtSecret =
    Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["JwtSettings:Secret"]
    ?? "pethub_secret_key_default_local_dev_12345";

// ==================================================================
// 2. SERVICE REGISTRATION (Dependency Injection)
// ==================================================================

// Database Context (MySQL / TiDB)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// SignalR (Real-time Chat)
builder.Services.AddSignalR();

// HTTP Client for External APIs (ViaCEP)
builder.Services.AddHttpClient<CepService>();

// CORS: Allow Frontend to access Backend
var allowedOrigins = frontendUrl.Split([';', ','], StringSplitOptions.RemoveEmptyEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials(); // Required for SignalR
        }
    );
});

// API Controllers
builder.Services.AddControllers();

// Swagger Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ==================================================================
// 3. MIDDLEWARE PIPELINE
// ==================================================================

var app = builder.Build();

// Enable Swagger in Development mode
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Apply CORS Policy (Must be before Authorization)
app.UseCors("AllowFrontend");

app.UseAuthorization();

// Map API Endpoints
app.MapControllers();

// Map SignalR Hubs
app.MapHub<ChatHub>("/chatHub");

// Start Application
app.Run();
