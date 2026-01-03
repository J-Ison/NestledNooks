using System;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace NestledNooks.Services
{
    public sealed class SmtpEmailService : IEmailService
    {
        private readonly SmtpOptions _options;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<SmtpOptions> options, ILogger<SmtpEmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendContactEmail(string name, string fromEmail, string message)
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

            using var client = new MailKit.Net.Smtp.SmtpClient();
            try
            {
                SecureSocketOptions socketOptions = _options.Security switch
                {
                    SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
                    SmtpSecurity.None => SecureSocketOptions.None,
                    _ => _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None
                };

                _logger.LogInformation("Connecting to SMTP {Host}:{Port} (User:{User}) using {SocketOptions}", _options.Host, _options.Port, string.IsNullOrEmpty(_options.Username) ? "(none)" : _options.Username, socketOptions);

                await client.ConnectAsync(_options.Host, _options.Port, socketOptions).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(_options.Username))
                {
                    await client.AuthenticateAsync(_options.Username, _options.Password).ConfigureAwait(false);
                }

                await client.SendAsync(mime).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send contact email (Host={Host},Port={Port},User={User})", _options.Host, _options.Port, string.IsNullOrEmpty(_options.Username) ? "(none)" : _options.Username);
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
}
