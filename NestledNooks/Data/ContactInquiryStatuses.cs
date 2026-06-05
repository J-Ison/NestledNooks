namespace NestledNooks.Data;

/// <summary>Workflow states for the owner contact-inquiry inbox (not in-app guest messaging).</summary>
public static class ContactInquiryStatuses
{
    public const string New = "New";
    public const string Read = "Read";
    public const string Replied = "Replied";
    public const string Archived = "Archived";

    public static readonly IReadOnlyList<string> All =
    [
        New,
        Read,
        Replied,
        Archived,
    ];
}
