using Microsoft.EntityFrameworkCore;

namespace PetHub.API.Data;

public static class DatabaseInitializer
{
    public const string ApplyMigrationsVariable = "APPLY_MIGRATIONS";
    public const string ResetDatabaseVariable = "RESET_DATABASE";

    public static async Task InitializeAsync(WebApplication app)
    {
        if (app.Environment.IsEnvironment("Testing"))
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope
            .ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseInitializer");

        try
        {
            if (context.Database.IsRelational() && IsEnabled(ResetDatabaseVariable))
            {
                await DropAllTablesAsync(context, logger);
            }

            if (context.Database.IsRelational() && ShouldApplyMigrations(app.Environment))
            {
                logger.LogInformation("Applying database migrations");
                await context.Database.MigrateAsync();
            }
            else if (context.Database.IsRelational())
            {
                logger.LogInformation(
                    "Skipping database migrations. Set {Variable}=true after a schema change",
                    ApplyMigrationsVariable
                );
            }

            await DbSeeder.SeedCatalogAsync(context);

            if (app.Environment.IsDevelopment())
            {
                logger.LogInformation("Seeding development demo data");
                await DbSeeder.SeedDemoDataAsync(context);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }

    /// <summary>
    /// Production/Docker skip migrations unless APPLY_MIGRATIONS=true.
    /// RESET_DATABASE=true also applies migrations after dropping tables.
    /// Development still applies them so local `dotnet run` stays convenient.
    /// </summary>
    public static bool ShouldApplyMigrations(IHostEnvironment environment)
    {
        if (IsEnabled(ApplyMigrationsVariable) || IsEnabled(ResetDatabaseVariable))
        {
            return true;
        }

        var raw = Environment.GetEnvironmentVariable(ApplyMigrationsVariable);
        if (!string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return environment.IsDevelopment();
    }

    public static bool IsEnabled(string variableName)
    {
        var raw = Environment.GetEnvironmentVariable(variableName);
        return raw is not null
            && (
                raw.Equals("true", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("1", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("yes", StringComparison.OrdinalIgnoreCase)
            );
    }

    private static async Task DropAllTablesAsync(AppDbContext context, ILogger logger)
    {
        logger.LogWarning(
            "{Variable}=true: dropping all tables in the current database",
            ResetDatabaseVariable
        );

        await context.Database.OpenConnectionAsync();
        try
        {
            await context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0");

            var tableNames = new List<string>();
            await using (var command = context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText =
                    """
                    SELECT table_name
                    FROM information_schema.tables
                    WHERE table_schema = DATABASE()
                      AND table_type = 'BASE TABLE'
                    """;

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tableNames.Add(reader.GetString(0));
                }
            }

            foreach (var table in tableNames)
            {
                var escaped = table.Replace("`", "``", StringComparison.Ordinal);
                await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS `" + escaped + "`");
            }

            await context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1");
            logger.LogWarning("Dropped {Count} tables", tableNames.Count);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }
}
