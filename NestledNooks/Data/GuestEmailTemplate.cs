namespace NestledNooks.Data;

/// <summary>Reusable guest email snippet for host quick-send from Manage bookings.</summary>
public sealed class GuestEmailTemplate
{
    public int Id { get; set; }

    /// <summary>Property slug, or empty for all properties.</summary>
    public string PropertySlug { get; set; } = "";

    public string Category { get; set; } = GuestEmailTemplateCategories.General;

    public string Title { get; set; } = "";

    /// <summary>Optional email subject override. Tokens supported.</summary>
    public string? EmailSubject { get; set; }

    public string Body { get; set; } = "";

    public int SortOrder { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
