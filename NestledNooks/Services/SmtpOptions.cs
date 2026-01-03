namespace NestledNooks.Services
{
    public enum SmtpSecurity
    {
        StartTls,
        SslOnConnect,
        None
    }

    public sealed class SmtpOptions
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public SmtpSecurity Security { get; set; } = SmtpSecurity.StartTls;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromName { get; set; } = "Nestled Nooks";
        public string FromEmail { get; set; } = "no-reply@localhost";
        public string ToEmail { get; set; } = "apple5stays@gmail.com";
    }
}
