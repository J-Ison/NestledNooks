using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NestledNooks.Data;

namespace NestledNooks.Services;

public sealed class SiteThemePreviewAccessor(IHttpContextAccessor httpContextAccessor) : ISiteThemePreviewAccessor
{
    public const string CookieName = "nn_theme_preview";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Task<SiteTheme?> GetActivePreviewAsync(CancellationToken cancellationToken = default)
    {
        var payload = ReadPayload();
        if (payload is null)
            return Task.FromResult<SiteTheme?>(null);

        if (payload.ExpiresUtc <= DateTime.UtcNow)
            return Task.FromResult<SiteTheme?>(null);

        return Task.FromResult<SiteTheme?>(payload.Theme);
    }

    public Task<SiteThemePreviewStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var payload = ReadPayload();
        if (payload is null || payload.ExpiresUtc <= DateTime.UtcNow)
            return Task.FromResult(new SiteThemePreviewStatus(false, null));

        return Task.FromResult(new SiteThemePreviewStatus(true, payload.ExpiresUtc));
    }

    public Task SetPreviewAsync(SiteTheme theme, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var context = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Theme preview requires an active HTTP request.");

        if (context.Response.HasStarted)
            throw new InvalidOperationException("Theme preview cannot be set after the HTTP response has started.");

        var clone = SiteThemeCopy.Clone(theme);
        clone.Id = 1;

        var payload = new ThemePreviewCookiePayload
        {
            Theme = clone,
            ExpiresUtc = DateTime.UtcNow.Add(duration),
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        context.Response.Cookies.Append(CookieName, json, BuildCookieOptions(context, duration));
        return Task.CompletedTask;
    }

    public Task ClearPreviewAsync(CancellationToken cancellationToken = default)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null || context.Response.HasStarted)
            return Task.CompletedTask;

        context.Response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
        });

        return Task.CompletedTask;
    }

    private ThemePreviewCookiePayload? ReadPayload()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
            return null;

        if (!context.Request.Cookies.TryGetValue(CookieName, out var json) || string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ThemePreviewCookiePayload>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static CookieOptions BuildCookieOptions(HttpContext context, TimeSpan duration) =>
        new()
        {
            HttpOnly = true,
            IsEssential = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = duration.Add(TimeSpan.FromSeconds(30)),
        };

    private sealed class ThemePreviewCookiePayload
    {
        public SiteTheme Theme { get; set; } = new();
        public DateTime ExpiresUtc { get; set; }
    }
}

internal static class SiteThemeCopy
{
    public static SiteTheme Clone(SiteTheme source) => new()
    {
        Id = source.Id,
        PresetKey = source.PresetKey,
        PrimaryColor = source.PrimaryColor,
        PrimaryLightColor = source.PrimaryLightColor,
        PrimarySoftBg = source.PrimarySoftBg,
        PrimaryBorderColor = source.PrimaryBorderColor,
        PrimaryTextColor = source.PrimaryTextColor,
        AccentColor = source.AccentColor,
        AccentBorderColor = source.AccentBorderColor,
        HeroStartColor = source.HeroStartColor,
        HeroMidColor = source.HeroMidColor,
        HeroEndColor = source.HeroEndColor,
        HeroBorderColor = source.HeroBorderColor,
        BookingColor = source.BookingColor,
        BookingDarkColor = source.BookingDarkColor,
        PageBgTop = source.PageBgTop,
        PageBgBottom = source.PageBgBottom,
        UpdatedAtUtc = source.UpdatedAtUtc,
    };
}
