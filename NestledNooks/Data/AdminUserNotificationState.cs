namespace NestledNooks.Data;

public sealed class AdminUserNotificationState
{
    public string UserId { get; set; } = "";
    public ApplicationUser? User { get; set; }

    public DateTime? UsersSectionSeenAtUtc { get; set; }
}
