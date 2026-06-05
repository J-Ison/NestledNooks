namespace NestledNooks.Services;

public static class UserDisplayNames
{
    public static string Format(string? email, string? userName, string? nickname)
    {
        if (!string.IsNullOrWhiteSpace(nickname))
            return nickname.Trim();

        if (!string.IsNullOrWhiteSpace(email))
            return email;

        if (!string.IsNullOrWhiteSpace(userName))
            return userName;

        return "User";
    }

    public static bool IsConfiguredOwnerEmail(string? email, IEnumerable<string> ownerEmails)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return ownerEmails.Any(e =>
            string.Equals(e.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
