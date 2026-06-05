namespace NestledNooks;

/// <summary>Public site name and taglines (header, titles, SEO).</summary>
public static class SiteBranding
{
    public const string Name = "Nestled Nooks";

    /// <summary>Default message tag for the owner when no custom tags are set.</summary>
    public const string DefaultOwnerTag = "Host";
    public const string RegionLine = "Black Hills vacation rentals";
    public const string Tagline = "Relax • Recharge • Reset";
    public const string HeaderSubtitle = "Black Hills · Relax • Recharge • Reset";

    public const string DefaultPageTitleSuffix = "Nestled Nooks · Black Hills";
    public const string DefaultMetaDescription =
        "Nestled Nooks offers Black Hills vacation rentals in South Dakota. Book Deerfield Retreat — a peaceful stay near Rapid City.";

    public static string PageTitle(string page) => $"{page} · {DefaultPageTitleSuffix}";
}
