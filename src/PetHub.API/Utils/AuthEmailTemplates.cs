namespace PetHub.API.Utils;

public static class AuthEmailTemplates
{
    public const string TokenMarker = "use this token: ";

    public const string VerificationSubject = "Verify your PetHub email";

    public const string PasswordResetSubject = "Reset your PetHub password";

    public static (string Text, string Html) Verification(
        string name,
        string verifyUrl,
        string token,
        int expiresHours
    )
    {
        var text = $"""
            Hello {name},

            Confirm your PetHub email by opening this link:
            {verifyUrl}

            If the link does not work, {TokenMarker}{token}

            This link expires in {expiresHours} hours.
            """;

        var html = $"""
            <p>Hello {System.Net.WebUtility.HtmlEncode(name)},</p>
            <p>Confirm your PetHub email by <a href="{verifyUrl}">clicking this link</a>.</p>
            <p>If the link does not work, {TokenMarker}<strong>{System.Net.WebUtility.HtmlEncode(token)}</strong></p>
            <p>This link expires in {expiresHours} hours.</p>
            """;

        return (text, html);
    }

    public static (string Text, string Html) PasswordReset(
        string name,
        string resetUrl,
        string token,
        int expiresHours
    )
    {
        var text = $"""
            Hello {name},

            Reset your PetHub password by opening this link:
            {resetUrl}

            If the link does not work, {TokenMarker}{token}

            This link expires in {expiresHours} hours. If you did not request a reset, you can ignore this email.
            """;

        var html = $"""
            <p>Hello {System.Net.WebUtility.HtmlEncode(name)},</p>
            <p>Reset your PetHub password by <a href="{resetUrl}">clicking this link</a>.</p>
            <p>If the link does not work, {TokenMarker}<strong>{System.Net.WebUtility.HtmlEncode(token)}</strong></p>
            <p>This link expires in {expiresHours} hours. If you did not request a reset, you can ignore this email.</p>
            """;

        return (text, html);
    }

    public static string? ExtractToken(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        var index = body.IndexOf(TokenMarker, StringComparison.Ordinal);
        if (index < 0)
            return null;

        var start = index + TokenMarker.Length;
        var remaining = body[start..];
        var end = remaining.IndexOfAny(['<', '\r', '\n']);
        var token = end >= 0 ? remaining[..end] : remaining;
        return token.Trim();
    }
}
