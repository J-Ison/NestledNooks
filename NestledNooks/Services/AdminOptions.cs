namespace NestledNooks.Services;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    /// <summary>Accounts that receive the Owner role (manage bookings, approve/deny, payment).</summary>
    public List<string> OwnerEmails { get; set; } = [];
}
