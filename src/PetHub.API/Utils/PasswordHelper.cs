namespace PetHub.API.Utils;

public static class PasswordHelper
{
    // Hashes a password using BCrypt
    public static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    // Verifies if the provided password matches the stored hash
    // We CANNOT decrypt the hash, we can only verify against it.
    public static bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
