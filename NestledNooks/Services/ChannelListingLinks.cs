using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace NestledNooks.Services;

public static class ChannelListingLinks
{
    public static string? BuildAirbnbUrl(
        string? listingUrl,
        DateOnly checkIn,
        DateOnly checkOut,
        int adults = 2) =>
        BuildUrl(
            listingUrl,
            new Dictionary<string, string?>
            {
                ["check_in"] = checkIn.ToString("yyyy-MM-dd"),
                ["check_out"] = checkOut.ToString("yyyy-MM-dd"),
                ["adults"] = adults.ToString(),
            });

    public static string? BuildVrboUrl(
        string? listingUrl,
        DateOnly checkIn,
        DateOnly checkOut,
        int adults = 2) =>
        BuildUrl(
            listingUrl,
            new Dictionary<string, string?>
            {
                ["chkin"] = checkIn.ToString("yyyy-MM-dd"),
                ["chkout"] = checkOut.ToString("yyyy-MM-dd"),
                ["adults"] = adults.ToString(),
            },
            removeKeys: ["dateless"]);

    private static string? BuildUrl(
        string? listingUrl,
        IReadOnlyDictionary<string, string?> setParams,
        IReadOnlyList<string>? removeKeys = null)
    {
        if (string.IsNullOrWhiteSpace(listingUrl))
            return null;

        if (!Uri.TryCreate(listingUrl.Trim(), UriKind.Absolute, out var uri))
            return null;

        var query = QueryHelpers.ParseQuery(uri.Query);
        foreach (var key in removeKeys ?? [])
            query.Remove(key);

        foreach (var (key, value) in setParams)
        {
            if (!string.IsNullOrWhiteSpace(value))
                query[key] = value;
        }

        var builder = new UriBuilder(uri)
        {
            Query = BuildQueryString(query),
        };

        return builder.Uri.ToString();
    }

    private static string BuildQueryString(Dictionary<string, StringValues> query)
    {
        var pairs = query
            .SelectMany(kvp => kvp.Value.Select(value => (kvp.Key, value)))
            .Where(pair => !string.IsNullOrEmpty(pair.value))
            .Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.value!)}");

        return string.Join("&", pairs);
    }
}
