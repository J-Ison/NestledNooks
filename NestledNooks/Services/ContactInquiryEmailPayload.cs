namespace NestledNooks.Services;

public sealed class ContactInquiryEmailPayload
{
    public int InquiryId { get; init; }
    public string DisplayName { get; init; } = "";
    public string ReplyEmail { get; init; } = "";
    public string Message { get; init; } = "";
    public bool IsVerifiedAccount { get; init; }
    public string? SubmittedByUserId { get; init; }
}
