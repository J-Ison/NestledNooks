using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class ChannelListingLinksTests
{
    [Fact]
    public void BuildAirbnbUrl_AppendsCheckInAndCheckOut()
    {
        var url = ChannelListingLinks.BuildAirbnbUrl(
            "https://airbnb.com/h/mydeerfieldretreat",
            new DateOnly(2026, 6, 24),
            new DateOnly(2026, 6, 26));

        Assert.NotNull(url);
        Assert.Contains("check_in=2026-06-24", url, StringComparison.Ordinal);
        Assert.Contains("check_out=2026-06-26", url, StringComparison.Ordinal);
        Assert.Contains("adults=2", url, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildVrboUrl_AppendsDatesAndRemovesDateless()
    {
        var url = ChannelListingLinks.BuildVrboUrl(
            "https://www.vrbo.com/4507873?dateless=true",
            new DateOnly(2026, 6, 24),
            new DateOnly(2026, 6, 26));

        Assert.NotNull(url);
        Assert.Contains("chkin=2026-06-24", url, StringComparison.Ordinal);
        Assert.Contains("chkout=2026-06-26", url, StringComparison.Ordinal);
        Assert.DoesNotContain("dateless=", url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildAirbnbUrl_ReturnsNullForMissingUrl()
    {
        var url = ChannelListingLinks.BuildAirbnbUrl(
            null,
            new DateOnly(2026, 6, 24),
            new DateOnly(2026, 6, 26));

        Assert.Null(url);
    }
}
