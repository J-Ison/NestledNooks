using NestledNooks.Services;

namespace NestledNooks.Tests.Infrastructure;

/// <summary>
/// Records outbound emails in memory so tests can assert notifications without SMTP.
/// </summary>
public sealed class FakeEmailService : IEmailService
{
    public List<ContactInquiryEmailPayload> ContactInquiryEmails { get; } = [];
    public List<(string Name, string FromEmail, string Message)> LegacyContactEmails { get; } = [];

    public Task SendContactEmail(string name, string fromEmail, string message)
    {
        LegacyContactEmails.Add((name, fromEmail, message));
        return Task.CompletedTask;
    }

    public Task SendContactInquiryEmail(ContactInquiryEmailPayload payload)
    {
        ContactInquiryEmails.Add(payload);
        return Task.CompletedTask;
    }

    public Task SendBookingRequestEmail(BookingRequestEmailPayload payload) => Task.CompletedTask;

    public Task SendBookingRequestGuestConfirmationEmail(BookingRequestEmailPayload payload) => Task.CompletedTask;

    public Task SendBookingStatusChangedEmailsAsync(BookingStatusEmailPayload payload) => Task.CompletedTask;

    public Task SendTemporaryPasswordEmailAsync(
        string toEmail,
        string userName,
        string temporaryPassword,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
