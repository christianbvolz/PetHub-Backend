namespace PetHub.API.Utils;

/// <summary>
/// Helper class for generating UUIDs (v7) for the application
/// </summary>
public static class UuidHelper
{
    /// <summary>
    /// Generates a new UUID v7 optimized for MySQL/TiDB databases.
    /// UUID v7 is time-ordered (sortable by creation time) and prevents enumeration attacks.
    /// </summary>
    /// <returns>A new Guid (UUID v7) suitable for database primary keys</returns>
    public static Guid NewId()
    {
        // Using PostgreSql parameter because MySQL stores UUIDs as char(36), same as PostgreSQL
        // UUIDNext.Database.PostgreSql generates time-ordered UUIDs compatible with MySQL/MariaDB/TiDB
        return UUIDNext.Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql);
    }
}
