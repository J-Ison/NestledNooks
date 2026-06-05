using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class UserNicknamesTests
{
    [Fact]
    public void ValidateRegistrationName_AcceptsFirstAndLastName()
    {
        var result = UserNicknames.ValidateRegistrationName("  Jane   Smith  ");

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal("Jane Smith", result.Normalized);
    }

    [Fact]
    public void ValidateRegistrationName_AcceptsSingleWord()
    {
        var result = UserNicknames.ValidateRegistrationName("Jane");

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.Equal("Jane", result.Normalized);
    }

    [Fact]
    public void ValidateRegistrationName_RejectsThreeWords()
    {
        var result = UserNicknames.ValidateRegistrationName("Mary Jane Watson");

        Assert.False(result.Succeeded, "Registration allows at most two words.");
        Assert.Contains("one word or first and last", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("O'Brien Smith")]
    [InlineData("Jean-Luc Picard")]
    public void ValidateRegistrationName_AllowsHyphenAndApostrophe(string name)
    {
        var result = UserNicknames.ValidateRegistrationName(name);

        Assert.True(result.Succeeded, result.ErrorMessage);
    }
}
