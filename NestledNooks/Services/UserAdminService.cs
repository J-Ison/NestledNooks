using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NestledNooks.Data;

namespace NestledNooks.Services;

public interface IUserAdminService
{
    Task<IReadOnlyList<UserAdminListItem>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<AdminPasswordResetResult> ResetPasswordAndNotifyAsync(
        string userId,
        string actingUserId,
        CancellationToken cancellationToken = default);
}

public sealed class UserAdminListItem
{
    public required string Id { get; init; }
    public string? Email { get; init; }
    public string? UserName { get; init; }
    public string? PhoneNumber { get; init; }
    public bool EmailConfirmed { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public bool IsLockedOut { get; init; }
}

public sealed record AdminPasswordResetResult(
    bool Succeeded,
    bool EmailSent,
    string? TemporaryPassword,
    string? Message,
    string? ErrorMessage);

public sealed class UserAdminService(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    ILogger<UserAdminService> logger) : IUserAdminService
{
    public async Task<IReadOnlyList<UserAdminListItem>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users
            .OrderBy(u => u.Email)
            .ThenBy(u => u.UserName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = new List<UserAdminListItem>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
            items.Add(new UserAdminListItem
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.OrderBy(r => r).ToList(),
                IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
            });
        }

        return items;
    }

    public async Task<AdminPasswordResetResult> ResetPasswordAndNotifyAsync(
        string userId,
        string actingUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new AdminPasswordResetResult(false, false, null, null, "User id is required.");

        var user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
            return new AdminPasswordResetResult(false, false, null, null, "User not found.");

        if (string.IsNullOrWhiteSpace(user.Email))
            return new AdminPasswordResetResult(false, false, null, null, "User has no email address on file.");

        var temporaryPassword = await TemporaryPasswordGenerator.GenerateAsync(userManager, user).ConfigureAwait(false);
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
        var resetResult = await userManager.ResetPasswordAsync(user, resetToken, temporaryPassword).ConfigureAwait(false);

        if (!resetResult.Succeeded)
        {
            var errors = string.Join(" ", resetResult.Errors.Select(e => e.Description));
            return new AdminPasswordResetResult(false, false, null, null, errors);
        }

        logger.LogInformation(
            "Owner {ActingUserId} reset password for user {UserId} ({Email})",
            actingUserId,
            user.Id,
            user.Email);

        try
        {
            await emailService.SendTemporaryPasswordEmailAsync(
                user.Email,
                user.UserName ?? user.Email,
                temporaryPassword,
                cancellationToken).ConfigureAwait(false);

            return new AdminPasswordResetResult(
                true,
                true,
                null,
                $"New password emailed to {user.Email}.",
                null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Password reset for {Email} succeeded but email failed", user.Email);
            return new AdminPasswordResetResult(
                true,
                false,
                temporaryPassword,
                null,
                $"Password was updated, but the email could not be sent ({ex.Message}). Share the temporary password with the user manually.");
        }
    }
}

internal static class TemporaryPasswordGenerator
{
    private const string Lower = "abcdefghjkmnpqrstuvwxyz";
    private const string Upper = "ABCDEFGHJKMNPQRSTUVWXYZ";
    private const string Digits = "23456789";
    private const string Special = "!@#$%&*";

    public static async Task<string> GenerateAsync(UserManager<ApplicationUser> userManager, ApplicationUser user)
    {
        var options = userManager.Options.Password;
        var length = Math.Max(options.RequiredLength, 12);

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var candidate = GenerateCandidate(length, options);
            var valid = true;

            foreach (var validator in userManager.PasswordValidators)
            {
                var result = await validator.ValidateAsync(userManager, user, candidate).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
                return candidate;
        }

        throw new InvalidOperationException("Could not generate a password that meets policy requirements.");
    }

    private static string GenerateCandidate(int length, PasswordOptions options)
    {
        var required = new List<char>();
        if (options.RequireLowercase) required.Add(Pick(Lower));
        if (options.RequireUppercase) required.Add(Pick(Upper));
        if (options.RequireDigit) required.Add(Pick(Digits));
        if (options.RequireNonAlphanumeric) required.Add(Pick(Special));

        var pool = Lower + Upper + Digits + (options.RequireNonAlphanumeric ? Special : "");
        while (required.Count < length)
            required.Add(Pick(pool));

        Shuffle(required);
        return new string(required.Take(length).ToArray());
    }

    private static char Pick(string from)
    {
        var index = RandomNumberGenerator.GetInt32(from.Length);
        return from[index];
    }

    private static void Shuffle(List<char> chars)
    {
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
    }
}
