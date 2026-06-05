using NestledNooks.Data;
using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

/// <summary>
/// Covers role-to-tag mapping and custom tag overrides shown in messaging UI.
/// </summary>
public sealed class UserMessageTagsTests
{
    [Theory]
    [InlineData(AppRoles.Owner, "Host")]
    [InlineData(AppRoles.CoHost, "Co-Host")]
    [InlineData(AppRoles.Manager, "Manager")]
    [InlineData(AppRoles.Client, "Guest")]
    public void DisplayTagForRole_MapsApplicationRolesToFriendlyLabels(string role, string expectedTag)
    {
        Assert.Equal(expectedTag, UserMessageTags.DisplayTagForRole(role));
    }

    [Fact]
    public void TagsFromRoles_OwnerAlsoShowsGuestWhenMultipleRolesPresent()
    {
        var tags = UserMessageTags.TagsFromRoles([AppRoles.Owner, AppRoles.Client]);
        Assert.Contains("Host", tags);
        Assert.Contains("Guest", tags);
    }

    [Fact]
    public void ResolveForDisplay_CustomTagsReplaceRoleTags()
    {
        var json = UserMessageTags.Serialize(["VIP", "Repeat guest"]);
        Assert.NotNull(json);

        var tags = UserMessageTags.ResolveForDisplay(json, [AppRoles.Owner]);
        Assert.Equal(["VIP", "Repeat guest"], tags);
        Assert.DoesNotContain("Host", tags);
    }

    [Fact]
    public void NormalizeInput_RejectsTagsLongerThanMaxLength()
    {
        var tooLong = new string('x', UserMessageTags.MaxTagLength + 1);
        var result = UserMessageTags.NormalizeInput([tooLong]);

        Assert.False(result.Succeeded, "Over-length tags should fail normalization.");
        Assert.Contains("24 characters", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NormalizeInput_RejectsMoreThanMaxTags()
    {
        var tags = Enumerable.Range(1, UserMessageTags.MaxTags + 1).Select(i => $"Tag{i}").ToList();
        var result = UserMessageTags.NormalizeInput(tags);

        Assert.False(result.Succeeded, $"More than {UserMessageTags.MaxTags} tags should fail.");
        Assert.Contains("at most", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_InvalidJsonReturnsEmptyListInsteadOfThrowing()
    {
        var tags = UserMessageTags.Parse("{not valid json");
        Assert.Empty(tags);
    }
}
