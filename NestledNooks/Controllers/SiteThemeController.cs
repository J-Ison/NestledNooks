using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestledNooks.Data;
using NestledNooks.Services;

namespace NestledNooks.Controllers;

[Authorize(Roles = AppRoles.Owner)]
[Route("api/site-theme")]
public class SiteThemeController(ISiteThemeService siteThemeService) : Controller
{
    [HttpPost("preview")]
    public async Task<IActionResult> StartPreview([FromForm] SiteTheme theme, [FromForm] string? returnUrl = null)
    {
        await siteThemeService.StartPreviewAsync(theme).ConfigureAwait(false);
        return LocalRedirect(SafeReturnUrl(returnUrl, "/"));
    }

    [HttpPost("preview/end")]
    public async Task<IActionResult> EndPreview([FromForm] string? returnUrl = null)
    {
        await siteThemeService.ClearPreviewAsync().ConfigureAwait(false);
        return LocalRedirect(SafeReturnUrl(returnUrl, "/"));
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromForm] SiteTheme theme, [FromForm] string? returnUrl = null)
    {
        try
        {
            await siteThemeService.SaveAsync(theme).ConfigureAwait(false);
            return LocalRedirect(WithQuery(SafeReturnUrl(returnUrl, "/account/manage/site"), "saved", "1"));
        }
        catch (Exception)
        {
            return LocalRedirect(WithQuery(SafeReturnUrl(returnUrl, "/account/manage/site"), "error", "save"));
        }
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset([FromForm] string? returnUrl = null)
    {
        try
        {
            await siteThemeService.ResetToDefaultAsync().ConfigureAwait(false);
            return LocalRedirect(WithQuery(SafeReturnUrl(returnUrl, "/account/manage/site"), "reset", "1"));
        }
        catch (Exception)
        {
            return LocalRedirect(WithQuery(SafeReturnUrl(returnUrl, "/account/manage/site"), "error", "reset"));
        }
    }

    private static string SafeReturnUrl(string? returnUrl, string fallback)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return fallback;

        if (Uri.TryCreate(returnUrl, UriKind.Relative, out _) && returnUrl.StartsWith('/'))
            return returnUrl;

        return fallback;
    }

    private static string WithQuery(string path, string key, string value)
    {
        var separator = path.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{path}{separator}{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
    }
}
