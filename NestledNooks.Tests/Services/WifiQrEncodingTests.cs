using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class WifiQrEncodingTests
{
    [Fact]
    public void BuildPayload_EncodesDeerfieldRetreatWifi()
    {
        var payload = WifiQrEncoding.BuildPayload("Apple5Wifi_Guest", "EnjoyYourStay88");

        Assert.Equal("WIFI:T:WPA;S:Apple5Wifi_Guest;P:EnjoyYourStay88;;", payload);
    }

    [Fact]
    public void BuildPayload_EscapesSpecialCharacters()
    {
        var payload = WifiQrEncoding.BuildPayload("Net;Name", "pass,word");

        Assert.Equal(@"WIFI:T:WPA;S:Net\;Name;P:pass\,word;;", payload);
    }
}
