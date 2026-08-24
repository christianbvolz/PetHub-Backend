using Microsoft.EntityFrameworkCore;

namespace PetHub.API.Data;

public static class DatabaseDefaults
{
    /// <summary>
    /// Fixed MySQL 8 server version so EF Core does not need a live database
    /// to detect the version at startup or when generating migrations.
    /// TiDB Cloud is MySQL 8 compatible.
    /// </summary>
    public static readonly ServerVersion MySqlServerVersion = new MySqlServerVersion(
        new Version(8, 0, 21)
    );
}
