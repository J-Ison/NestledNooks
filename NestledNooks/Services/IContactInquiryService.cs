using NestledNooks.Data;

namespace NestledNooks.Services;

public sealed class ContactInquirySubmitRequest
{
    /// <summary>Authenticated user id, or null for anonymous public form submissions.</summary>
    public string? ActingUserId { get; init; }

    public string? SelfReportedName { get; init; }
    public string? SelfReportedEmail { get; init; }
    public string Message { get; init; } = "";
}

public sealed class ContactInquirySubmitResult
{
    public bool Succeeded { get; init; }
    public int? InquiryId { get; init; }
    public string? ErrorMessage { get; init; }

    public static ContactInquirySubmitResult Ok(int inquiryId) =>
        new() { Succeeded = true, InquiryId = inquiryId };

    public static ContactInquirySubmitResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}

public sealed class ContactInquiryListItem
{
    public int Id { get; init; }
    public DateTime SubmittedAtUtc { get; init; }
    public string DisplayName { get; init; } = "";
    public string ReplyEmail { get; init; } = "";
    public string Preview { get; init; } = "";
    public bool IsVerifiedAccount { get; init; }
    public string Status { get; init; } = ContactInquiryStatuses.New;
}

public sealed class ContactInquiryDetail
{
    public int Id { get; init; }
    public DateTime SubmittedAtUtc { get; init; }
    public string DisplayName { get; init; } = "";
    public string ReplyEmail { get; init; } = "";
    public string Message { get; init; } = "";
    public bool IsVerifiedAccount { get; init; }
    public string? SubmittedByUserId { get; init; }
    public string Status { get; init; } = ContactInquiryStatuses.New;
    public DateTime? ReadAtUtc { get; init; }
    public string? OwnerNotes { get; init; }
}

public sealed class ContactInquiryUpdateRequest
{
    public string Status { get; init; } = ContactInquiryStatuses.New;
    public string? OwnerNotes { get; init; }
}

public interface IContactInquiryService
{
    Task<ContactInquirySubmitResult> SubmitAsync(
        ContactInquirySubmitRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContactInquiryListItem>> GetInboxAsync(
        string statusFilter = "all",
        CancellationToken cancellationToken = default);

    Task<ContactInquiryDetail?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    Task<ContactInquirySubmitResult> UpdateAsync(
        int id,
        ContactInquiryUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task MarkReadAsync(int id, CancellationToken cancellationToken = default);
}
