using System.Text.Json;
using NestledNooks.Data;

namespace NestledNooks.Services;

public static class UserMessageTags
{
    public const int MaxTags = 5;
    public const int MaxTagLength = 24;

    private static readonly string[] RoleTagOrder =
    [
        AppRoles.Owner,
        AppRoles.CoHost,
        AppRoles.Manager,
        AppRoles.Client,
    ];

    public static string DisplayTagForRole(string role) => role switch
    {
        AppRoles.Owner => SiteBranding.DefaultOwnerTag,
        AppRoles.CoHost => "Co-Host",
        AppRoles.Manager => "Manager",
        AppRoles.Client => "Guest",
        _ => role,
    };

    public static IReadOnlyList<string> TagsFromRoles(IEnumerable<string> roles)
    {
        var roleSet = roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var tags = new List<string>();
        foreach (var role in RoleTagOrder)
        {
            if (roleSet.Contains(role))
                tags.Add(DisplayTagForRole(role));
        }

        if (tags.Count == 0)
            tags.Add(DisplayTagForRole(AppRoles.Client));

        return tags;
    }

    /// <summary>Custom tags replace role-based tags when any are saved.</summary>
    public static IReadOnlyList<string> ResolveForDisplay(string? messageTagsJson, IEnumerable<string> roles)
    {
        var custom = Parse(messageTagsJson);
        if (custom.Count > 0)
            return custom;

        return TagsFromRoles(roles);
    }

    public static IReadOnlyList<string> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var tags = JsonSerializer.Deserialize<List<string>>(json);
            if (tags is null)
                return [];

            return tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxTags)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public static string? Serialize(IReadOnlyList<string> tags)
    {
        var normalized = NormalizeInput(tags);
        if (!normalized.Succeeded)
            return null;

        return normalized.Tags!.Count == 0
            ? null
            : JsonSerializer.Serialize(normalized.Tags);
    }

    public static TagNormalizeResult NormalizeInput(IEnumerable<string>? tags)
    {
        if (tags is null)
            return new TagNormalizeResult(true, [], null);

        var result = new List<string>();
        foreach (var raw in tags)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var tag = raw.Trim();
            if (tag.Length > MaxTagLength)
                return new TagNormalizeResult(false, null, $"Tags cannot be longer than {MaxTagLength} characters.");

            if (result.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
                continue;

            result.Add(tag);
            if (result.Count > MaxTags)
                return new TagNormalizeResult(false, null, $"You can have at most {MaxTags} tags.");
        }

        return new TagNormalizeResult(true, result, null);
    }

    public sealed record TagNormalizeResult(bool Succeeded, IReadOnlyList<string>? Tags, string? ErrorMessage);
}
