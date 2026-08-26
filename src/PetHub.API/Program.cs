using System.IdentityModel.Tokens.Jwt;
using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PetHub.API.Configuration;
using PetHub.API.Data;
using PetHub.API.Hubs;
using PetHub.API.Middlewares;
using PetHub.API.Services;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    // Load .env file from project root (local) or current directory (container)
    var root = Directory.GetCurrentDirectory();
    var envCandidates = new[]
    {
        Path.Combine(root, "..", "..", ".env"),
        Path.Combine(root, ".env"),
    };
    var envFile = envCandidates.FirstOrDefault(File.Exists);
    if (envFile is not null)
    {
        Env.Load(envFile);
    }

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog(
        (context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName();

            if (context.HostingEnvironment.IsProduction())
            {
                loggerConfiguration.WriteTo.Console(new CompactJsonFormatter());
            }
            else
            {
                loggerConfiguration.WriteTo.Console();
            }
        }
    );

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

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // Database Context (MySQL / TiDB)
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(connectionString, DatabaseDefaults.MySqlServerVersion)
    );

    builder
        .Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("database");

    builder.Services.AddPetHubRateLimiting(builder.Configuration);

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

    // Refresh Token Configuration (Options Pattern)
    builder
        .Services.AddOptions<PetHub.API.Configuration.RefreshTokenSettings>()
        .Bind(
            builder.Configuration.GetSection(PetHub.API.Configuration.RefreshTokenSettings.SectionName)
        )
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddSingleton(TimeProvider.System);

    builder
        .Services.AddOptions<AuthLifecycleSettings>()
        .Bind(builder.Configuration.GetSection(AuthLifecycleSettings.SectionName))
        .Configure(options =>
        {
            var frontend =
                Environment.GetEnvironmentVariable("FRONTEND_URL") ?? options.FrontendBaseUrl;
            var firstOrigin = frontend
                .Split([';', ','], StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()
                ?.Trim();
            if (!string.IsNullOrWhiteSpace(firstOrigin))
            {
                options.FrontendBaseUrl = firstOrigin;
            }
        })
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder
        .Services.AddOptions<SmtpSettings>()
        .Bind(builder.Configuration.GetSection(SmtpSettings.SectionName))
        .Configure(options =>
        {
            options.Host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? options.Host;
            if (int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var smtpPort))
            {
                options.Port = smtpPort;
            }
            options.User = Environment.GetEnvironmentVariable("SMTP_USER") ?? options.User;
            options.Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? options.Password;
            options.FromEmail = Environment.GetEnvironmentVariable("SMTP_FROM") ?? options.FromEmail;
            options.FromName =
                Environment.GetEnvironmentVariable("SMTP_FROM_NAME") ?? options.FromName;
        });

    // Cloudinary Configuration (Options Pattern)
    var cloudinaryOptionsBuilder = builder
        .Services.AddOptions<PetHub.API.Configuration.CloudinarySettings>()
        .Bind(builder.Configuration.GetSection("Cloudinary"))
        .Configure(options =>
        {
            // Allow CloudName, ApiKey and ApiSecret to be provided via environment variables for security
            options.CloudName =
                Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME") ?? options.CloudName;
            options.ApiKey = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY") ?? options.ApiKey;
            options.ApiSecret =
                Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET") ?? options.ApiSecret;
        });

    // Only validate Cloudinary settings on startup in Production
    if (builder.Environment.IsProduction())
    {
        cloudinaryOptionsBuilder.ValidateDataAnnotations().ValidateOnStart();
    }

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
                // Use JWT `sub` as the Name claim so libraries that read User.Identity.Name
                // will receive the subject (the user id). This avoids emitting duplicate
                // claims in the token (we keep `sub` only in JwtService).
                NameClaimType = JwtRegisteredClaimNames.Sub,
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
                // SignalR (especially WebSockets) cannot send an Authorization header, so the
                // access token is passed as a query string during the hub handshake.
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (
                        !string.IsNullOrEmpty(accessToken)
                        && (
                            path.StartsWithSegments("/chatHub")
                            || path.StartsWithSegments("/notificationHub")
                        )
                    )
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },
            };
        });

    // Custom Services (Repositories, etc.)
    builder.Services.AddScoped<IPetRepository, PetRepository>();
    builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IAdoptionRequestRepository, AdoptionRequestRepository>();
    builder.Services.AddScoped<IAdoptionService, AdoptionService>();
    builder.Services.AddScoped<IChatService, ChatService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
    builder.Services.AddSingleton<IJwtService, JwtService>(); // Singleton: stateless service, thread-safe
    builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
    builder.Services.AddSingleton<IEmailSender, EmailSender>();
    builder.Services.AddScoped<IAuthLifecycleService, AuthLifecycleService>();

    // Cookie options provider (reads RefreshTokenSettings)
    builder.Services.AddSingleton<
        PetHub.API.Utils.ICookieOptionsProvider,
        PetHub.API.Utils.CookieOptionsProvider
    >();

    // Background Services
    builder.Services.AddHostedService<RefreshTokenCleanupService>();

    // ==================================================================
    // 3. MIDDLEWARE PIPELINE
    // ==================================================================

    var app = builder.Build();

    app.UseForwardedHeaders();

    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (httpContext, _, exception) =>
        {
            if (exception is not null)
            {
                return LogEventLevel.Error;
            }

            return httpContext.Request.Path.StartsWithSegments("/health")
                ? LogEventLevel.Debug
                : LogEventLevel.Information;
        };
    });

    // Enable Swagger in Development mode
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        app.UseHttpsRedirection();
    }

    // --- GLOBAL EXCEPTION HANDLER ---
    // This middleware catches any error from the code below it
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // --------------------------------

    // Apply CORS Policy (Must be before Authentication/Authorization)
    app.UseCors("AllowFrontend");

    // Enable Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.UseRateLimiter();
    }

    // Map API Endpoints
    app.MapControllers();

    // Map SignalR Hubs
    app.MapHub<ChatHub>("/chatHub").RequireAuthorization();
    app.MapHub<NotificationHub>("/notificationHub").RequireAuthorization();

    app.MapHealthChecks("/health").AllowAnonymous().DisableRateLimiting();

    await DatabaseInitializer.InitializeAsync(app);

    if (DatabaseInitializer.IsEnabled("MIGRATE_THEN_EXIT"))
    {
        Log.Information("Database reset/migrate finished; exiting because MIGRATE_THEN_EXIT=true");
        return;
    }

    // Start Application
    app.Run(); // Required for WebApplicationFactory in integration tests
}
catch (HostAbortedException)
{
    throw;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program { }
