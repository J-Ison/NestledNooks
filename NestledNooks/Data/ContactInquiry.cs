namespace NestledNooks.Data;

/// <summary>
/// Public contact-form submission stored separately from authenticated in-app messages.
/// When <see cref="SubmittedByUserId"/> is set, the inquiry is verified and tied to that account.
/// </summary>
public class ContactInquiry
{
    public int Id { get; set; }

    public DateTime SubmittedAtUtc { get; set; }

    /// <summary>Display name shown to the owner (from account or self-reported).</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>Reply-to email (account email when verified; otherwise self-reported).</summary>
    public string ReplyEmail { get; set; } = "";

    public string Message { get; set; } = "";

    /// <summary>Set when the submitter was signed in; null for anonymous inquiries.</summary>
    public string? SubmittedByUserId { get; set; }

    public ApplicationUser? SubmittedByUser { get; set; }

    /// <summary>True when tied to a signed-in account (not self-reported identity).</summary>
    public bool IsVerifiedAccount { get; set; }

    public string Status { get; set; } = ContactInquiryStatuses.New;

    public DateTime? ReadAtUtc { get; set; }

    public string? OwnerNotes { get; set; }
}
