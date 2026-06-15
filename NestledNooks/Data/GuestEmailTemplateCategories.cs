namespace NestledNooks.Data;

public static class GuestEmailTemplateCategories
{
    public const string CheckIn = "check-in";
    public const string CheckOut = "check-out";
    public const string Payment = "payment";
    public const string Welcome = "welcome";
    public const string General = "general";

    public static IReadOnlyList<string> All { get; } =
    [
        CheckIn,
        CheckOut,
        Payment,
        Welcome,
        General,
    ];

    public static string DisplayName(string category) => category switch
    {
        CheckIn => "Check-in",
        CheckOut => "Check-out",
        Payment => "Payment",
        Welcome => "Welcome",
        _ => "General",
    };
}
