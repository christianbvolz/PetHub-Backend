using Microsoft.EntityFrameworkCore;

namespace PetHub.API.Data;

public static class DatabaseInitializer
{
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
            if (context.Database.IsRelational())
            {
                logger.LogInformation("Applying database migrations");
                await context.Database.MigrateAsync();
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
}
