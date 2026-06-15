namespace NestledNooks.Services;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public bool Enabled { get; set; }

    public string? PublishableKey { get; set; }

    public string? SecretKey { get; set; }

    public string? WebhookSecret { get; set; }

    /// <summary>Minimum deposit when approving with deposit (percent of booking total).</summary>
    public int DefaultMinimumDepositPercent { get; set; } = 50;

    public bool IsConfigured =>
        Enabled &&
        !string.IsNullOrWhiteSpace(SecretKey) &&
        !string.IsNullOrWhiteSpace(PublishableKey);

    public bool IsTestMode =>
        PublishableKey?.StartsWith("pk_test_", StringComparison.Ordinal) == true ||
        SecretKey?.StartsWith("sk_test_", StringComparison.Ordinal) == true;
}
