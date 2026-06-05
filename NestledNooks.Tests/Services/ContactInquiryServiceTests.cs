using NestledNooks.Data;
using NestledNooks.Services;
using NestledNooks.Tests.Infrastructure;

namespace NestledNooks.Tests.Services;

/// <summary>
/// Verifies public contact inquiries are stored separately from in-app messaging
/// and that signed-in users cannot impersonate another identity on the form.
/// </summary>
public sealed class ContactInquiryServiceTests
{
    [Fact]
    public async Task SubmitAsync_Anonymous_SavesUnverifiedInquiryAndNotifiesOwner()
    {
        await using var scope = await TestServiceScope.CreateAsync();

        var result = await scope.ContactInquiries.SubmitAsync(new ContactInquirySubmitRequest
        {
            SelfReportedName = "Jane Guest",
            SelfReportedEmail = "jane@example.com",
            Message = "Do you allow late check-in?",
        });

        Assert.True(
            result.Succeeded,
            $"Anonymous submit should succeed, but failed with: {result.ErrorMessage ?? "(no message)"}.");
        Assert.NotNull(result.InquiryId);

        var saved = await scope.Db.ContactInquiries.FindAsync(result.InquiryId!.Value);
        Assert.NotNull(saved);
        Assert.False(saved!.IsVerifiedAccount, "Anonymous inquiries must not be marked as verified account submissions.");
        Assert.Null(saved.SubmittedByUserId);
        Assert.Equal("jane@example.com", saved.ReplyEmail);
        Assert.Equal(ContactInquiryStatuses.New, saved.Status);

        Assert.Single(scope.EmailService.ContactInquiryEmails);
        Assert.Equal(result.InquiryId, scope.EmailService.ContactInquiryEmails[0].InquiryId);
        Assert.False(scope.EmailService.ContactInquiryEmails[0].IsVerifiedAccount);
    }

    [Fact]
    public async Task SubmitAsync_SignedInUser_UsesAccountIdentityAndIgnoresSelfReportedFields()
    {
        await using var scope = await TestServiceScope.CreateAsync();
        var user = await scope.CreateUserAsync("real.user@example.com", nickname: "Real Nickname");

        var result = await scope.ContactInquiries.SubmitAsync(new ContactInquirySubmitRequest
        {
            ActingUserId = user.Id,
            SelfReportedName = "Fake Owner Name",
            SelfReportedEmail = "impersonator@evil.com",
            Message = "Question from my verified account.",
        });

        Assert.True(
            result.Succeeded,
            $"Verified submit should succeed, but failed with: {result.ErrorMessage ?? "(no message)"}.");

        var saved = await scope.Db.ContactInquiries.FindAsync(result.InquiryId!.Value);
        Assert.NotNull(saved);
        Assert.True(saved!.IsVerifiedAccount, "Signed-in submissions must be marked verified so the owner can trust the identity.");
        Assert.Equal(user.Id, saved.SubmittedByUserId);
        Assert.Equal("real.user@example.com", saved.ReplyEmail);
        Assert.Equal("Real Nickname", saved.DisplayName);
        Assert.DoesNotContain("impersonator", saved.ReplyEmail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitAsync_RejectsMessageShorterThanFiveCharacters()
    {
        await using var scope = await TestServiceScope.CreateAsync();

        var result = await scope.ContactInquiries.SubmitAsync(new ContactInquirySubmitRequest
        {
            SelfReportedName = "Jane Guest",
            SelfReportedEmail = "jane@example.com",
            Message = "Hi",
        });

        Assert.False(result.Succeeded, "Messages under 5 characters should be rejected.");
        Assert.Contains("5 characters", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Empty(scope.Db.ContactInquiries);
    }

    [Fact]
    public async Task SubmitAsync_RejectsAnonymousSubmissionWithoutValidEmail()
    {
        await using var scope = await TestServiceScope.CreateAsync();

        var result = await scope.ContactInquiries.SubmitAsync(new ContactInquirySubmitRequest
        {
            SelfReportedName = "Jane Guest",
            SelfReportedEmail = "not-an-email",
            Message = "Valid message body here.",
        });

        Assert.False(result.Succeeded, "Anonymous submit with invalid email should fail.");
        Assert.Contains("valid email", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetInboxAsync_FiltersByStatus()
    {
        await using var scope = await TestServiceScope.CreateAsync();

        scope.Db.ContactInquiries.AddRange(
            new ContactInquiry
            {
                SubmittedAtUtc = DateTime.UtcNow,
                DisplayName = "New One",
                ReplyEmail = "a@test.com",
                Message = "First message",
                Status = ContactInquiryStatuses.New,
            },
            new ContactInquiry
            {
                SubmittedAtUtc = DateTime.UtcNow.AddMinutes(-1),
                DisplayName = "Archived One",
                ReplyEmail = "b@test.com",
                Message = "Second message",
                Status = ContactInquiryStatuses.Archived,
            });
        await scope.Db.SaveChangesAsync();

        var archived = await scope.ContactInquiries.GetInboxAsync(ContactInquiryStatuses.Archived);
        Assert.Single(archived);
        Assert.Equal("Archived One", archived[0].DisplayName);
    }

    [Fact]
    public async Task MarkReadAsync_UpdatesStatusAndReadTimestamp()
    {
        await using var scope = await TestServiceScope.CreateAsync();

        var inquiry = new ContactInquiry
        {
            SubmittedAtUtc = DateTime.UtcNow,
            DisplayName = "Reader Test",
            ReplyEmail = "reader@test.com",
            Message = "Please mark me read.",
            Status = ContactInquiryStatuses.New,
        };
        scope.Db.ContactInquiries.Add(inquiry);
        await scope.Db.SaveChangesAsync();

        await scope.ContactInquiries.MarkReadAsync(inquiry.Id);

        var updated = await scope.Db.ContactInquiries.FindAsync(inquiry.Id);
        Assert.NotNull(updated);
        Assert.Equal(ContactInquiryStatuses.Read, updated!.Status);
        Assert.NotNull(updated.ReadAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_RejectsUnknownStatusWithClearError()
    {
        await using var scope = await TestServiceScope.CreateAsync();

        var inquiry = new ContactInquiry
        {
            SubmittedAtUtc = DateTime.UtcNow,
            DisplayName = "Status Test",
            ReplyEmail = "status@test.com",
            Message = "Status workflow test.",
            Status = ContactInquiryStatuses.New,
        };
        scope.Db.ContactInquiries.Add(inquiry);
        await scope.Db.SaveChangesAsync();

        var result = await scope.ContactInquiries.UpdateAsync(inquiry.Id, new ContactInquiryUpdateRequest
        {
            Status = "NotARealStatus",
        });

        Assert.False(result.Succeeded, "Unknown workflow status should be rejected.");
        Assert.Contains("Unknown status", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }
}
