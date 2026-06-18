namespace NestledNooks.Models;

public sealed record StayPriceComparison(
    BookingQuote Direct,
    ChannelPriceEstimate Airbnb,
    ChannelPriceEstimate Vrbo);

public sealed record ChannelPriceEstimate(
    string Channel,
    decimal? TotalAmount,
    decimal? NightlyAverage,
    bool UsesLiveRates,
    string? Note)
{
    public static ChannelPriceEstimate Unavailable(string channel, string note) =>
        new(channel, null, null, false, note);
}
