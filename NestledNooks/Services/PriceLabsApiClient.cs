using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace NestledNooks.Services;

public sealed class PriceLabsApiClient(
    HttpClient httpClient,
    IOptions<PriceLabsOptions> options,
    ILogger<PriceLabsApiClient> logger) : IPriceLabsApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<IReadOnlyList<PriceLabsListingInfo>> GetListingsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, "listings", cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var listings = new List<PriceLabsListingInfo>();
        foreach (var item in EnumerateListingObjects(doc.RootElement))
        {
            var id = ReadString(item, "id", "listing_id", "listingId");
            if (string.IsNullOrWhiteSpace(id))
                continue;

            listings.Add(new PriceLabsListingInfo
            {
                ListingId = id,
                Name = ReadString(item, "name", "listing_name", "listingName", "title"),
                Pms = ReadString(item, "pms", "PMS"),
            });
        }

        return listings;
    }

    public async Task<IReadOnlyList<PriceLabsDayPrice>> GetListingPricesAsync(
        string listingId,
        string pms,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(listingId) || string.IsNullOrWhiteSpace(pms))
            return [];

        var requestBody = JsonSerializer.Serialize(new
        {
            listings = new[]
            {
                new
                {
                    id = listingId.Trim(),
                    pms = pms.Trim().ToLowerInvariant(),
                    start_date = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    end_date = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                },
            },
        }, JsonOptions);

        using var response = await SendAsync(
            HttpMethod.Post,
            "listing_prices",
            cancellationToken,
            body: requestBody).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (doc.RootElement.ValueKind == JsonValueKind.Object &&
            doc.RootElement.TryGetProperty("error", out var errorProp))
        {
            var message = ReadString(doc.RootElement, "desc", "description", "message") ?? errorProp.ToString();
            throw new HttpRequestException($"PriceLabs API error: {message}");
        }

        var prices = new List<PriceLabsDayPrice>();
        foreach (var listing in EnumerateListingPriceResponses(doc.RootElement))
        {
            if (listing.ValueKind != JsonValueKind.Object)
                continue;

            if (listing.TryGetProperty("error", out _))
                continue;

            if (!listing.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var item in data.EnumerateArray())
            {
                if (!TryReadDate(item, out var date))
                    continue;

                if (!TryReadDecimal(item, out var rate, "price", "rate", "recommended_price", "recommendedPrice", "amount"))
                    continue;

                prices.Add(new PriceLabsDayPrice
                {
                    Date = date,
                    Rate = rate,
                    MinimumStay = ReadInt(item, "min_stay", "minStay", "minimum_stay", "minimumStay"),
                });
            }
        }

        logger.LogInformation(
            "PriceLabs returned {Count} nightly rates for listing {ListingId}/{Pms} ({Start} to {End}).",
            prices.Count,
            listingId,
            pms,
            startDate,
            endDate);

        return prices;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string?>? query = null,
        string? body = null)
    {
        var opts = options.Value;
        var apiKey = opts.ApiKey?.Trim();
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("PriceLabs API key is not configured.");

        var baseUrl = (string.IsNullOrWhiteSpace(opts.BaseUrl) ? "https://api.pricelabs.co/v1" : opts.BaseUrl).TrimEnd('/');
        var relative = path.TrimStart('/');
        var url = $"{baseUrl}/{relative}";

        if (query is not null)
        {
            var qs = string.Join("&",
                query.Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
                    .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}"));
            if (qs.Length > 0)
                url += "?" + qs;
        }

        using var request = new HttpRequestMessage(method, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-API-Key", apiKey);

        if (!string.IsNullOrWhiteSpace(body))
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        return await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new HttpRequestException(
            $"PriceLabs API returned {(int)response.StatusCode}: {Truncate(body, 500)}");
    }

    private static IEnumerable<JsonElement> EnumerateListingObjects(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                yield return item;
            yield break;
        }

        if (root.ValueKind != JsonValueKind.Object)
            yield break;

        foreach (var propertyName in new[] { "listings", "data", "results" })
        {
            if (root.TryGetProperty(propertyName, out var nested) && nested.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in nested.EnumerateArray())
                    yield return item;
                yield break;
            }
        }

        yield return root;
    }

    private static IEnumerable<JsonElement> EnumerateListingPriceResponses(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                yield return item;
            yield break;
        }

        if (root.ValueKind == JsonValueKind.Object)
            yield return root;
    }

    private static bool TryReadDate(JsonElement element, out DateOnly date)
    {
        date = default;
        var raw = ReadString(element, "date", "day", "start_date", "startDate");
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
        {
            date = DateOnly.FromDateTime(dt);
            return true;
        }

        return false;
    }

    private static bool TryReadDecimal(JsonElement element, out decimal value, params string[] names)
    {
        value = 0;
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var prop))
                continue;

            switch (prop.ValueKind)
            {
                case JsonValueKind.Number when prop.TryGetDecimal(out value):
                    return true;
                case JsonValueKind.String when decimal.TryParse(
                    prop.GetString(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out value):
                    return true;
            }
        }

        return false;
    }

    private static string? ReadString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var prop))
                continue;

            if (prop.ValueKind == JsonValueKind.String)
                return prop.GetString();

            if (prop.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                return prop.ToString();
        }

        return null;
    }

    private static int? ReadInt(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var prop))
                continue;

            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var value))
                return value;

            if (prop.ValueKind == JsonValueKind.String &&
                int.TryParse(prop.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return value;
        }

        return null;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";
}
