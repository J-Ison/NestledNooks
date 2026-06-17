using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class BookingStayDatesTests
{
    [Fact]
    public void RangesOverlap_detects_wrapping_stay()
    {
        Assert.True(BookingStayDates.RangesOverlap(
            new DateOnly(2026, 8, 3),
            new DateOnly(2026, 8, 15),
            new DateOnly(2026, 8, 4),
            new DateOnly(2026, 8, 14)));
    }

    [Fact]
    public void FindChannelClosureStart_detects_long_airbnb_unavailable_block()
    {
        var today = new DateOnly(2026, 6, 1);
        var closureStart = new DateOnly(2026, 12, 14);

        var result = BookingStayDates.FindChannelClosureStart(
        [
            (closureStart, closureStart.AddDays(365)),
        ],
        today);

        Assert.Equal(closureStart, result);
    }

    [Fact]
    public void FindChannelClosureStart_ignores_short_reservation_blocks()
    {
        var today = new DateOnly(2026, 6, 1);

        var result = BookingStayDates.FindChannelClosureStart(
        [
            (new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5)),
        ],
        today);

        Assert.Null(result);
    }
}
