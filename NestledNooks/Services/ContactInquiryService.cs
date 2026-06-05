using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using NestledNooks.Data;

namespace NestledNooks.Services;

public sealed class ContactInquiryService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    ILogger<ContactInquiryService> logger) : IContactInquiryService
{
    private const int MaxMessageLength = 4000;

    public async Task<ContactInquirySubmitResult> SubmitAsync(
        ContactInquirySubmitRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedMessage = NormalizeMessage(request.Message);
        if (normalizedMessage is null)
            return ContactInquirySubmitResult.Fail("Please enter a message (at least 5 characters).");

        string displayName;
        string replyEmail;
        string? submittedByUserId;
        bool isVerified;

        if (!string.IsNullOrWhiteSpace(request.ActingUserId))
        {
            var user = await userManager.FindByIdAsync(request.ActingUserId).ConfigureAwait(false);
            if (user is null)
                return ContactInquirySubmitResult.Fail("Your signed-in account could not be verified. Please sign in again.");

            displayName = UserDisplayNames.Format(user.Email, user.UserName, user.Nickname);
            replyEmail = user.Email?.Trim() ?? user.UserName ?? "";
            if (string.IsNullOrWhiteSpace(replyEmail))
                return ContactInquirySubmitResult.Fail("Your account does not have an email address on file.");

            submittedByUserId = user.Id;
            isVerified = true;
        }
        else
        {
            displayName = request.SelfReportedName?.Trim() ?? "";
            replyEmail = request.SelfReportedEmail?.Trim() ?? "";
            if (displayName.Length < 2)
                return ContactInquirySubmitResult.Fail("Please enter your name.");
            if (string.IsNullOrWhiteSpace(replyEmail) || !replyEmail.Contains('@'))
                return ContactInquirySubmitResult.Fail("Please enter a valid email address.");

            submittedByUserId = null;
            isVerified = false;
        }

        var inquiry = new ContactInquiry
        {
            SubmittedAtUtc = DateTime.UtcNow,
            DisplayName = displayName,
            ReplyEmail = replyEmail,
            Message = normalizedMessage,
            SubmittedByUserId = submittedByUserId,
            IsVerifiedAccount = isVerified,
            Status = ContactInquiryStatuses.New,
        };

        db.ContactInquiries.Add(inquiry);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await emailService.SendContactInquiryEmail(new ContactInquiryEmailPayload
            {
                InquiryId = inquiry.Id,
                DisplayName = inquiry.DisplayName,
                ReplyEmail = inquiry.ReplyEmail,
                Message = inquiry.Message,
                IsVerifiedAccount = inquiry.IsVerifiedAccount,
                SubmittedByUserId = inquiry.SubmittedByUserId,
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Inquiry is saved; email is a notification backup for the owner.
            logger.LogWarning(ex, "Contact inquiry #{InquiryId} saved but notification email failed.", inquiry.Id);
        }

        logger.LogInformation(
            "Contact inquiry #{InquiryId} submitted ({Verified}): {Email}",
            inquiry.Id,
            isVerified ? "verified account" : "anonymous form",
            replyEmail);

        return ContactInquirySubmitResult.Ok(inquiry.Id);
    }

    public async Task<IReadOnlyList<ContactInquiryListItem>> GetInboxAsync(
        string statusFilter = "all",
        CancellationToken cancellationToken = default)
    {
        var query = db.ContactInquiries.AsNoTracking();

        if (!string.Equals(statusFilter, "all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(i => i.Status == statusFilter);

        var rows = await query
            .OrderByDescending(i => i.SubmittedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(i => new ContactInquiryListItem
            {
                Id = i.Id,
                SubmittedAtUtc = i.SubmittedAtUtc,
                DisplayName = i.DisplayName,
                ReplyEmail = i.ReplyEmail,
                Preview = TruncatePreview(i.Message),
                IsVerifiedAccount = i.IsVerifiedAccount,
                Status = i.Status,
            })
            .ToList();
    }

    public async Task<ContactInquiryDetail?> GetDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var inquiry = await db.ContactInquiries
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return inquiry is null ? null : MapDetail(inquiry);
    }

    public async Task<ContactInquirySubmitResult> UpdateAsync(
        int id,
        ContactInquiryUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ContactInquiryStatuses.All.Contains(request.Status))
            return ContactInquirySubmitResult.Fail($"Unknown status '{request.Status}'.");

        var inquiry = await db.ContactInquiries
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (inquiry is null)
            return ContactInquirySubmitResult.Fail("Inquiry not found.");

        inquiry.Status = request.Status;
        inquiry.OwnerNotes = string.IsNullOrWhiteSpace(request.OwnerNotes) ? null : request.OwnerNotes.Trim();

        if (request.Status == ContactInquiryStatuses.Read && inquiry.ReadAtUtc is null)
            inquiry.ReadAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ContactInquirySubmitResult.Ok(inquiry.Id);
    }

    public async Task MarkReadAsync(int id, CancellationToken cancellationToken = default)
    {
        var inquiry = await db.ContactInquiries
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (inquiry is null)
            return;

        if (inquiry.Status == ContactInquiryStatuses.New)
            inquiry.Status = ContactInquiryStatuses.Read;

        inquiry.ReadAtUtc ??= DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string? NormalizeMessage(string? message)
    {
        var trimmed = message?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length < 5)
            return null;

        return trimmed.Length <= MaxMessageLength ? trimmed : trimmed[..MaxMessageLength];
    }

    private static string TruncatePreview(string body)
    {
        var singleLine = body.ReplaceLineEndings(" ").Trim();
        return singleLine.Length <= 120 ? singleLine : singleLine[..117] + "…";
    }

    private static ContactInquiryDetail MapDetail(ContactInquiry inquiry) => new()
    {
        Id = inquiry.Id,
        SubmittedAtUtc = inquiry.SubmittedAtUtc,
        DisplayName = inquiry.DisplayName,
        ReplyEmail = inquiry.ReplyEmail,
        Message = inquiry.Message,
        IsVerifiedAccount = inquiry.IsVerifiedAccount,
        SubmittedByUserId = inquiry.SubmittedByUserId,
        Status = inquiry.Status,
        ReadAtUtc = inquiry.ReadAtUtc,
        OwnerNotes = inquiry.OwnerNotes,
    };
}
