namespace NestledNooks.Services;

public interface IEmailService
{
    Task SendContactEmail(string name, string fromEmail, string message);

    Task SendContactInquiryEmail(ContactInquiryEmailPayload payload);

    Task SendBookingRequestEmail(BookingRequestEmailPayload payload);

    Task SendBookingRequestGuestConfirmationEmail(BookingRequestEmailPayload payload);

    Task SendBookingStatusChangedEmailsAsync(BookingStatusEmailPayload payload);

    Task SendBookingPaymentRequestEmailAsync(BookingPaymentEmailPayload payload);

    Task SendBookingGuestMessageEmailAsync(BookingGuestMessageEmailPayload payload);

    Task SendTemporaryPasswordEmailAsync(
        string toEmail,
        string userName,
        string temporaryPassword,
        CancellationToken cancellationToken = default);
}
