using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NestledNooks.Data;

namespace NestledNooks.Services;

public static class OwnerRoleSeedService
{
    public static async Task EnsureOwnerUsersAsync(
        UserManager<ApplicationUser> userManager,
        IOptions<AdminOptions> adminOptions,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var emails = adminOptions.Value.OwnerEmails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (emails.Count == 0)
            return;

        foreach (var email in emails)
        {
            var user = await userManager.FindByEmailAsync(email).ConfigureAwait(false);
            if (user is null)
            {
                logger.LogInformation("Owner seed: no user registered yet for {Email}", email);
                continue;
            }

            if (await userManager.IsInRoleAsync(user, AppRoles.Owner).ConfigureAwait(false))
                continue;

            var result = await userManager.AddToRoleAsync(user, AppRoles.Owner).ConfigureAwait(false);
            if (result.Succeeded)
                logger.LogInformation("Assigned Owner role to {Email}", email);
            else
                logger.LogWarning(
                    "Could not assign Owner role to {Email}: {Errors}",
                    email,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    public static bool IsConfiguredOwnerEmail(string email, IOptions<AdminOptions> adminOptions)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return adminOptions.Value.OwnerEmails.Any(e =>
            string.Equals(e.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
