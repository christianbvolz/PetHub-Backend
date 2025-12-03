using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PetHub.API.Data;
using PetHub.API.Hubs;
using PetHub.API.Middlewares;
using PetHub.API.Services;

// Load .env file from project root
var root = Directory.GetCurrentDirectory();
var envFile = Path.Combine(root, "..", "..", ".env");
if (File.Exists(envFile))
{
    Env.Load(envFile);
}

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

// JWT Secret (REQUIRED - must be set in environment variable or .env file)
var jwtSecret =
    Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException(
        "JWT_SECRET environment variable is not set. Please configure it in your .env file or environment variables."
    );

// ==================================================================
// 2. SERVICE REGISTRATION (Dependency Injection)
// ==================================================================

// Database Context (MySQL / TiDB)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// SignalR (Real-time Chat)
builder.Services.AddSignalR();

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
builder.Services.AddSwaggerGen(options =>
{
    // API Information
    options.SwaggerDoc(
        "v1",
        new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "PetHub API",
            Version = "v1",
            Description =
                "API para conectar pessoas que desejam adotar animais de estimação com donos ou abrigos que possuem animais para adoção.",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "PetHub Team",
                Url = new Uri("https://github.com/christianbvolz/PetHub-Backend"),
            },
        }
    );

    // Include XML Comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // JWT Authentication
    options.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description =
                "JWT Authorization header using the Bearer scheme. Enter your token in the text input below. Example: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
        }
    );

    options.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                Array.Empty<string>()
            },
        }
    );
});

// JWT Configuration (Options Pattern)
builder
    .Services.AddOptions<PetHub.API.Configuration.JwtSettings>()
    .Bind(builder.Configuration.GetSection(PetHub.API.Configuration.JwtSettings.SectionName))
    .Configure(options =>
    {
        // Override SecretKey from environment variable (required for security)
        options.SecretKey = jwtSecret;
    })
    .ValidateDataAnnotations() // Validates [Required], [Range], etc.
    .ValidateOnStart(); // Fails fast on startup if configuration is invalid

// JWT Authentication
builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtIssuer = builder.Configuration["Jwt:Issuer"];
        var jwtAudience = builder.Configuration["Jwt:Audience"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            // ClockSkew uses the default value of 5 minutes. It is not currently configurable.
        };

        // Better error messages for development
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }
                return Task.CompletedTask;
            },
        };
    });

// Custom Services (Repositories, etc.)
builder.Services.AddScoped<IPetRepository, PetRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IJwtService, JwtService>(); // Singleton: stateless service, thread-safe

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

// --- GLOBAL EXCEPTION HANDLER ---
// This middleware catches any error from the code below it
app.UseMiddleware<GlobalExceptionMiddleware>();

// --------------------------------

// Apply CORS Policy (Must be before Authentication/Authorization)
app.UseCors("AllowFrontend");

// Enable Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map API Endpoints
app.MapControllers();

// Map SignalR Hubs
app.MapHub<ChatHub>("/chatHub");

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        await DbSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Start Application
app.Run(); // Required for WebApplicationFactory in integration tests

public partial class Program { }
