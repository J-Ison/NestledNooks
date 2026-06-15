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

    public Task SendContactInquiryEmail(ContactInquiryEmailPayload payload)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        mime.To.Add(MailboxAddress.Parse(_options.ToEmail));

        var trustLabel = payload.IsVerifiedAccount
            ? "Verified account inquiry"
            : "Unverified form inquiry";

        mime.Subject = $"[{trustLabel}] Contact #{payload.InquiryId} — {payload.DisplayName}";

        try
        {
            mime.ReplyTo.Add(MailboxAddress.Parse(payload.ReplyEmail));
        }
        catch
        {
            // Owner still receives the inquiry in-app even if reply-to is invalid.
        }

        var bodyBuilder = new BodyBuilder
        {
            TextBody =
                $"Contact inquiry #{payload.InquiryId}\r\n" +
                $"Trust: {(payload.IsVerifiedAccount ? "Verified signed-in account" : "Anonymous form — identity not verified")}\r\n" +
                (payload.IsVerifiedAccount && !string.IsNullOrEmpty(payload.SubmittedByUserId)
                    ? $"Account id: {payload.SubmittedByUserId}\r\n"
                    : "") +
                $"Name: {payload.DisplayName}\r\n" +
                $"Reply email: {payload.ReplyEmail}\r\n\r\n" +
                $"Message:\r\n{payload.Message}\r\n\r\n" +
                "View and manage this inquiry in Manage contact (owner tools)."
        };

        mime.Body = bodyBuilder.ToMessageBody();
        return SendMimeAsync(mime, "contact inquiry email");
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

    public Task SendBookingRequestGuestConfirmationEmail(BookingRequestEmailPayload payload)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        mime.To.Add(MailboxAddress.Parse(payload.GuestEmail));
        mime.Subject = $"We received your booking request {payload.BookingNumber}";

        var bodyBuilder = new BodyBuilder
        {
            TextBody =
                $"Hello {payload.GuestFullName},\r\n\r\n" +
                $"Thank you for your request at {payload.PropertyDisplayName}.\r\n\r\n" +
                $"Reference: {payload.BookingNumber}\r\n" +
                $"Check-in: {payload.CheckIn:yyyy-MM-dd}\r\n" +
                $"Check-out: {payload.CheckOut:yyyy-MM-dd}\r\n" +
                $"Nights: {payload.NightCount}\r\n" +
                $"Estimated total: {payload.TotalAmount:C2}\r\n" +
                $"Guests: {payload.GuestCount} · Pets: {payload.PetCount}\r\n\r\n" +
                (string.IsNullOrWhiteSpace(payload.Notes) ? "" : $"Your notes:\r\n{payload.Notes}\r\n\r\n") +
                "Status: Pending review. We will email you when your request is approved or declined.\r\n\r\n" +
                "— Nestled Nooks"
        };

        mime.Body = bodyBuilder.ToMessageBody();
        return SendMimeAsync(mime, "booking request confirmation (guest)");
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

    public Task SendBookingPaymentRequestEmailAsync(BookingPaymentEmailPayload payload)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        mime.To.Add(MailboxAddress.Parse(payload.GuestEmail));
        mime.Subject = $"Payment due — {payload.BookingNumber} ({payload.PropertyDisplayName})";

        var nonRefundableLine = payload.NonRefundable
            ? "\r\nThis deposit is non-refundable per the host approval terms.\r\n"
            : "";

        var bodyBuilder = new BodyBuilder
        {
            TextBody =
                $"Hello {payload.GuestFullName},\r\n\r\n" +
                $"Your stay at {payload.PropertyDisplayName} has been approved.\r\n\r\n" +
                $"Reference: {payload.BookingNumber}\r\n" +
                $"Dates: {payload.CheckIn:yyyy-MM-dd} to {payload.CheckOut:yyyy-MM-dd}\r\n" +
                $"Booking total: {payload.TotalAmount:C2}\r\n" +
                $"{payload.PaymentLabel}: {payload.AmountDue:C2}\r\n" +
                nonRefundableLine +
                "\r\nPay securely online:\r\n" +
                $"{payload.PaymentUrl}\r\n\r\n" +
                "— Nestled Nooks"
        };

        mime.Body = bodyBuilder.ToMessageBody();
        return SendMimeAsync(mime, "booking payment request (guest)");
    }

    public Task SendBookingGuestMessageEmailAsync(BookingGuestMessageEmailPayload payload)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        mime.To.Add(MailboxAddress.Parse(payload.GuestEmail));
        mime.Subject = string.IsNullOrWhiteSpace(payload.EmailSubject)
            ? $"Message about your booking {payload.BookingNumber}"
            : payload.EmailSubject.Trim();

        var bodyBuilder = new BodyBuilder { TextBody = payload.Message.Trim() };

        mime.Body = bodyBuilder.ToMessageBody();
        return SendMimeAsync(mime, "booking guest message");
    }

    public Task SendTemporaryPasswordEmailAsync(
        string toEmail,
        string userName,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        mime.To.Add(MailboxAddress.Parse(toEmail));
        mime.Subject = "Your Nestled Nooks password was reset";

        var bodyBuilder = new BodyBuilder
        {
            TextBody =
                $"Hello {userName},\r\n\r\n" +
                "An administrator reset your Nestled Nooks account password.\r\n\r\n" +
                $"Temporary password: {temporaryPassword}\r\n\r\n" +
                "Sign in with your email and this password, then change it under Account → Settings when you are ready.\r\n\r\n" +
                "If you did not expect this message, contact us right away.\r\n\r\n" +
                "— Nestled Nooks"
        };

        mime.Body = bodyBuilder.ToMessageBody();
        return SendMimeAsync(mime, "temporary password email");
    }

    private async Task SendMimeAsync(MimeMessage mime, string contextLabel)
    {
        if (!string.IsNullOrEmpty(_options.Username) && string.IsNullOrEmpty(_options.Password))
        {
            throw new InvalidOperationException(
                "SMTP password is not configured. Set Smtp:Password in user secrets (local development) " +
                "or application settings in Azure.");
        }

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
