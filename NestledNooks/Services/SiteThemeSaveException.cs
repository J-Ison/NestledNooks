namespace NestledNooks.Services;

internal static class SiteThemeSaveException
{
    public static string Describe(Exception ex)
    {
        var detail = ex.InnerException?.Message ?? ex.Message;
        if (detail.Contains("SiteThemes", StringComparison.OrdinalIgnoreCase)
            || detail.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
        {
            return $"{detail} Apply pending database migrations (dotnet ef database update), then try again.";
        }

        if (detail.Contains("identity column", StringComparison.OrdinalIgnoreCase))
        {
            return $"{detail} Run the latest database migration FixSiteThemeSingletonId, then try again.";
        }

        return detail;
    }
}
