using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NestledNooks.Data;

namespace NestledNooks.Services;

public static class OwnerRoleSeedService
{
    public static async Task EnsureRoleAssignmentsAsync(
        UserManager<ApplicationUser> userManager,
        IOptions<AdminOptions> adminOptions,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await EnsureOwnerUsersAsync(userManager, adminOptions, logger, cancellationToken)
            .ConfigureAwait(false);
        await EnsureDefaultGuestRoleAsync(userManager, logger, cancellationToken)
            .ConfigureAwait(false);
    }

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

            if (!await userManager.IsInRoleAsync(user, AppRoles.Owner).ConfigureAwait(false))
            {
                var result = await userManager.AddToRoleAsync(user, AppRoles.Owner).ConfigureAwait(false);
                if (result.Succeeded)
                    logger.LogInformation("Assigned Owner role to {Email}", email);
                else
                    logger.LogWarning(
                        "Could not assign Owner role to {Email}: {Errors}",
                        email,
                        string.Join("; ", result.Errors.Select(e => e.Description)));
            }

            if (await userManager.IsInRoleAsync(user, AppRoles.Client).ConfigureAwait(false))
            {
                var removeResult = await userManager.RemoveFromRoleAsync(user, AppRoles.Client)
                    .ConfigureAwait(false);
                if (removeResult.Succeeded)
                    logger.LogInformation("Removed Guest role from owner account {Email}", email);
            }
        }
    }

    public static async Task EnsureDefaultGuestRoleAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users.ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
            if (roles.Count > 0)
                continue;

            var result = await userManager.AddToRoleAsync(user, AppRoles.Client).ConfigureAwait(false);
            if (result.Succeeded)
                logger.LogInformation("Assigned Guest role to {Email}", user.Email ?? user.UserName);
            else
                logger.LogWarning(
                    "Could not assign Guest role to {Email}: {Errors}",
                    user.Email ?? user.UserName,
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
