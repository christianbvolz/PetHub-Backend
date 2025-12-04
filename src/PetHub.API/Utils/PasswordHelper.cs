namespace PetHub.API.Utils;

public static class PasswordHelper
{
    // Hashes a password using BCrypt
    public static string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException(
                "Password cannot be null, empty or whitespace.",
                nameof(password)
            );

        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    // Verifies if the provided password matches the stored hash
    // We CANNOT decrypt the hash, we can only verify against it.
    public static bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException(
                "Password cannot be null, empty or whitespace.",
                nameof(password)
            );

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException(
                "Password hash cannot be null, empty or whitespace.",
                nameof(passwordHash)
            );

        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
