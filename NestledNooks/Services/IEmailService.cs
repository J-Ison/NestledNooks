namespace NestledNooks.Services;

public interface IEmailService
{
    Task SendContactEmail(string name, string fromEmail, string message);

    Task SendBookingRequestEmail(BookingRequestEmailPayload payload);

    Task SendBookingStatusChangedEmailsAsync(BookingStatusEmailPayload payload);
}
