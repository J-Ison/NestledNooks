using System;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace NestledNooks.Services;

public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendContactEmail(string name, string fromEmail, string message)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        mime.To.Add(MailboxAddress.Parse(_options.ToEmail));
        mime.Subject = $"Website contact from {(string.IsNullOrWhiteSpace(name) ? fromEmail : name)}";

        var bodyBuilder = new BodyBuilder
        {
            TextBody = $"Name: {name}\nEmail: {fromEmail}\n\nMessage:\n{message}"
        };

        mime.Body = bodyBuilder.ToMessageBody();
        return SendMimeAsync(mime, "contact email");
    }

    public Task SendBookingRequestEmail(BookingRequestEmailPayload payload)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        mime.To.Add(MailboxAddress.Parse(_options.ToEmail));
        mime.Subject = $"New booking request {payload.BookingNumber} — {payload.PropertyDisplayName}";

        try
        {
            mime.ReplyTo.Add(MailboxAddress.Parse(payload.GuestEmail));
        }
        catch
        {
            // ignore invalid reply-to; To still receives the request
        }

        var bodyBuilder = new BodyBuilder
        {
            TextBody =
                $"New booking request {payload.BookingNumber} (ID {payload.RequestId})\r\n\r\n" +
                $"Property: {payload.PropertyDisplayName}\r\n" +
                $"Check-in: {payload.CheckIn:yyyy-MM-dd}\r\n" +
                $"Check-out: {payload.CheckOut:yyyy-MM-dd}\r\n" +
                $"Nights: {payload.NightCount}\r\n" +
                $"Estimated total: {payload.TotalAmount:C2}\r\n" +
                $"Guests: {payload.GuestCount}\r\n" +
                $"Pets: {payload.PetCount}\r\n\r\n" +
                $"Guest: {payload.GuestFullName}\r\n" +
                $"Email: {payload.GuestEmail}\r\n" +
                $"Phone: {(string.IsNullOrWhiteSpace(payload.GuestPhone) ? "(none)" : payload.GuestPhone)}\r\n\r\n" +
                $"Notes:\r\n{(string.IsNullOrWhiteSpace(payload.Notes) ? "(none)" : payload.Notes)}\r\n\r\n" +
                "Status: Pending — approve or deny in Manage bookings."
        };

        mime.Body = bodyBuilder.ToMessageBody();
        return SendMimeAsync(mime, "booking request email");
    }

    public async Task SendBookingStatusChangedEmailsAsync(BookingStatusEmailPayload payload)
    {
        var ownerBody =
            $"Booking {payload.BookingNumber} status changed.\r\n\r\n" +
            $"Property: {payload.PropertyDisplayName}\r\n" +
            $"Guest: {payload.GuestFullName} ({payload.GuestEmail})\r\n" +
            $"Dates: {payload.CheckIn:yyyy-MM-dd} → {payload.CheckOut:yyyy-MM-dd}\r\n" +
            $"Total: {payload.TotalAmount:C2}\r\n" +
            $"Status: {payload.OldStatus} → {payload.NewStatus}\r\n" +
            (string.IsNullOrWhiteSpace(payload.StatusNote) ? "" : $"Note: {payload.StatusNote}\r\n");

        var guestBody =
            $"Hello {payload.GuestFullName},\r\n\r\n" +
            $"Your booking request {payload.BookingNumber} for {payload.PropertyDisplayName} is now: {payload.NewStatus}.\r\n" +
            $"Dates: {payload.CheckIn:yyyy-MM-dd} to {payload.CheckOut:yyyy-MM-dd}\r\n" +
            $"Total: {payload.TotalAmount:C2}\r\n" +
            (string.IsNullOrWhiteSpace(payload.StatusNote) ? "" : $"\r\nMessage from host: {payload.StatusNote}\r\n");

        var ownerMime = new MimeMessage();
        ownerMime.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        ownerMime.To.Add(MailboxAddress.Parse(_options.ToEmail));
        ownerMime.Subject = $"Booking {payload.BookingNumber} — {payload.NewStatus}";
        ownerMime.Body = new BodyBuilder { TextBody = ownerBody }.ToMessageBody();
        await SendMimeAsync(ownerMime, "booking status email (owner)").ConfigureAwait(false);

        var guestMime = new MimeMessage();
        guestMime.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        guestMime.To.Add(MailboxAddress.Parse(payload.GuestEmail));
        guestMime.Subject = $"Your Nestled Nooks booking {payload.BookingNumber} — {payload.NewStatus}";
        guestMime.Body = new BodyBuilder { TextBody = guestBody }.ToMessageBody();
        await SendMimeAsync(guestMime, "booking status email (guest)").ConfigureAwait(false);
    }

    private async Task SendMimeAsync(MimeMessage mime, string contextLabel)
    {
        using var client = new MailKit.Net.Smtp.SmtpClient();
        try
        {
            var socketOptions = _options.Security switch
            {
                SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
                SmtpSecurity.None => SecureSocketOptions.None,
                _ => _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None
            };

            _logger.LogInformation(
                "Connecting to SMTP {Host}:{Port} (User:{User}) using {SocketOptions}",
                _options.Host,
                _options.Port,
                string.IsNullOrEmpty(_options.Username) ? "(none)" : _options.Username,
                socketOptions);

            await client.ConnectAsync(_options.Host, _options.Port, socketOptions).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password).ConfigureAwait(false);
            }

            await client.SendAsync(mime).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send {Context} (Host={Host},Port={Port},User={User})",
                contextLabel,
                _options.Host,
                _options.Port,
                string.IsNullOrEmpty(_options.Username) ? "(none)" : _options.Username);
            throw;
        }
        finally
        {
            try
            {
                await client.DisconnectAsync(true).ConfigureAwait(false);
            }
            catch
            {
                // ignore disconnect errors
            }
        }
    }
}
