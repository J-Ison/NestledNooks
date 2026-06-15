namespace NestledNooks.Data;

public static class BookingRequestActivity
{
    public static DateTime GetLastActivityUtc(BookingRequest booking) =>
        Max(booking.CreatedAtUtc, booking.StatusUpdatedAtUtc, booking.PaymentReceivedAtUtc);

    public static string DescribeUpdate(BookingRequest booking, DateTime? seenAtUtc)
    {
        if (seenAtUtc is null)
            return booking.Status == BookingStatuses.Pending ? "New request" : "New";

        var last = GetLastActivityUtc(booking);
        if (last <= seenAtUtc)
            return "";

        if (booking.PaymentReceivedAtUtc is { } paidAt && paidAt > seenAtUtc)
            return "Payment update";

        if (booking.StatusUpdatedAtUtc is { } statusAt && statusAt > seenAtUtc)
            return "Status change";

        return "Updated";
    }

    private static DateTime Max(DateTime created, DateTime? statusUpdated, DateTime? paymentReceived)
    {
        var max = created;
        if (statusUpdated is { } s && s > max)
            max = s;
        if (paymentReceived is { } p && p > max)
            max = p;
        return max;
    }
}
