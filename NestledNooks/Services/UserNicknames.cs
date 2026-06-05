using System.Text.RegularExpressions;

namespace NestledNooks.Services;

/// <summary>
/// Validates display nicknames shown in messaging and on the account profile.
/// Registration accepts one word or first and last name (up to two words).
/// </summary>
public static class UserNicknames
{
    public const int MaxLength = 50;

    private static readonly Regex CollapseWhitespace = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex NamePartPattern = new(@"^[\p{L}][\p{L}'-]*$", RegexOptions.Compiled);

    public sealed record ValidateResult(bool Succeeded, string? Normalized, string? ErrorMessage);

    public static ValidateResult ValidateRegistrationName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new ValidateResult(false, null, "Please enter a nickname.");

        var normalized = CollapseWhitespace.Replace(raw.Trim(), " ");
        if (normalized.Length > MaxLength)
            return new ValidateResult(false, null, $"Name cannot be longer than {MaxLength} characters.");

        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 1 or > 2)
            return new ValidateResult(
                false,
                null,
                "Enter one word or first and last name (for example, Jane or Jane Smith).");

        foreach (var part in parts)
        {
            if (!NamePartPattern.IsMatch(part))
            {
                return new ValidateResult(
                    false,
                    null,
                    "Names can only contain letters, hyphens, and apostrophes.");
            }
        }

        return new ValidateResult(true, normalized, null);
    }
}
