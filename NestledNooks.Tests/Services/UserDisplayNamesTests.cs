using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class UserDisplayNamesTests
{
    [Fact]
    public void Format_PrefersNicknameOverEmail()
    {
        var name = UserDisplayNames.Format("user@example.com", "user@example.com", "  Cabin Fan  ");
        Assert.Equal("Cabin Fan", name);
    }

    [Fact]
    public void Format_FallsBackToEmailWhenNicknameMissing()
    {
        var name = UserDisplayNames.Format("user@example.com", "user@example.com", null);
        Assert.Equal("user@example.com", name);
    }

    [Theory]
    [InlineData("apple5stays@gmail.com", true)]
    [InlineData("APPLE5STAYS@GMAIL.COM", true)]
    [InlineData("other@example.com", false)]
    public void IsConfiguredOwnerEmail_IsCaseInsensitive(string email, bool expected)
    {
        var owners = new[] { "apple5stays@gmail.com" };
        Assert.Equal(expected, UserDisplayNames.IsConfiguredOwnerEmail(email, owners));
    }
}
