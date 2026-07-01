using Microsoft.Extensions.Caching.Memory;

namespace NestledNooks.Services;

internal static class GuestDataCacheKeys
{
    public static string PropertyBySlug(string slug) => $"guest:property:slug:{slug}";

    public static string HomepageProperty => "guest:property:homepage";

    public static string SiteSettings => "guest:site-settings";

    public static string UnavailableDates(string slug, DateOnly from, DateOnly to, int? excludeBookingId) =>
        $"guest:unavailable:{slug}:{from:yyyyMMdd}:{to:yyyyMMdd}:{excludeBookingId?.ToString() ?? "none"}";

    public static void InvalidateProperty(IMemoryCache cache, string slug)
    {
        cache.Remove(PropertyBySlug(slug.Trim().ToLowerInvariant()));
        cache.Remove(HomepageProperty);
    }
}
