namespace NestledNooks.Services;

public static class GuestEmailWrapperDefaults
{
    public const string Header =
        "Hello {{GuestFullName}},";

    public const string Footer =
        "Reference: {{BookingNumber}}\r\n" +
        "Property: {{PropertyName}}\r\n" +
        "Dates: {{CheckIn}} to {{CheckOut}}\r\n\r\n" +
        "Reply to this email if you have questions.\r\n\r\n" +
        "— Nestled Nooks";
}
