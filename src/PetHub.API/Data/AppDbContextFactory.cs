using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PetHub.API.Data;

/// <summary>
/// Design-time factory used by <c>dotnet ef migrations</c> so migrations can be
/// generated without starting the web host or connecting to a live database.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? "Server=localhost;Port=3306;Database=pethub;Uid=root;Pwd=;";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(connectionString, DatabaseDefaults.MySqlServerVersion);

        return new AppDbContext(optionsBuilder.Options);
    }
}
