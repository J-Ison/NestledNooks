using System.Text.Json;

namespace NestledNooks.Data;

public sealed class PropertyBadge
{
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
}

public sealed class PropertyPhoto
{
    public string Url { get; set; } = "";
    public string Alt { get; set; } = "";
}

public static class PropertyContentJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static IReadOnlyList<string> ParseStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, Options) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string SerializeStringList(IEnumerable<string> values) =>
        JsonSerializer.Serialize(values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).ToList(), Options);

    public static IReadOnlyList<PropertyBadge> ParseBadges(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<PropertyBadge>>(json, Options) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string SerializeBadges(IEnumerable<PropertyBadge> badges) =>
        JsonSerializer.Serialize(
            badges
                .Where(b => !string.IsNullOrWhiteSpace(b.Title))
                .Select(b => new PropertyBadge { Title = b.Title.Trim(), Subtitle = b.Subtitle.Trim() })
                .ToList(),
            Options);

    public static IReadOnlyList<PropertyPhoto> ParsePhotos(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<PropertyPhoto>>(json, Options) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string SerializePhotos(IEnumerable<PropertyPhoto> photos) =>
        JsonSerializer.Serialize(
            photos
                .Where(p => !string.IsNullOrWhiteSpace(p.Url))
                .Select(p => new PropertyPhoto { Url = p.Url.Trim(), Alt = p.Alt.Trim() })
                .ToList(),
            Options);
}
