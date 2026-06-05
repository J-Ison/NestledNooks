using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class QrCodeServiceTests
{
    [Theory]
    [InlineData("https://backhillsnestlednooks.com/", "https://backhillsnestlednooks.com/")]
    [InlineData("backhillsnestlednooks.com", "https://backhillsnestlednooks.com/")]
    [InlineData("http://backhillsnestlednooks.com", "http://backhillsnestlednooks.com/")]
    public void NormalizeUrl_AcceptsHttpAndHttpsUrls(string input, string expected)
    {
        var service = new QrCodeService(null!, null!);
        var result = service.NormalizeUrl(input);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal(expected, result.NormalizedUrl);
    }

    [Fact]
    public void NormalizeUrl_RejectsInvalidScheme()
    {
        var service = new QrCodeService(null!, null!);
        var result = service.NormalizeUrl("ftp://example.com");

        Assert.False(result.Succeeded, "FTP URLs should be rejected for QR codes.");
    }

    [Fact]
    public void GeneratePng_ReturnsNonEmptyBytes()
    {
        var service = new QrCodeService(null!, null!);
        var png = service.GeneratePng("https://backhillsnestlednooks.com/");

        Assert.NotEmpty(png);
        Assert.Equal(0x89, png[0]);
        Assert.Equal(0x50, png[1]);
    }
}
