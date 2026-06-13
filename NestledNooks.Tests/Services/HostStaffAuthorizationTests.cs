using System.Security.Claims;
using NestledNooks.Data;
using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class HostStaffAuthorizationTests
{
    [Theory]
    [InlineData(AppRoles.Owner, true)]
    [InlineData(AppRoles.CoHost, true)]
    [InlineData(AppRoles.Manager, true)]
    [InlineData(AppRoles.Client, false)]
    public void IsHostStaff_RecognizesStaffRoles(string role, bool expected)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, role)],
            authenticationType: "test"));

        Assert.Equal(expected, HostStaffAuthorization.IsHostStaff(user));
    }

    [Fact]
    public void IsHostStaff_RejectsAnonymousUser()
    {
        Assert.False(HostStaffAuthorization.IsHostStaff(new ClaimsPrincipal()));
    }

    [Fact]
    public void EnsureHostStaff_ThrowsForGuestClient()
    {
        var client = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, AppRoles.Client)],
            authenticationType: "test"));

        Assert.Throws<UnauthorizedAccessException>(() => HostStaffAuthorization.EnsureHostStaff(client));
    }
}
