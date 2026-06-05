using NestledNooks.Data;
using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

/// <summary>
/// Documents who may start conversations in the in-app messaging system.
/// These are pure role rules — fast to run and easy to diagnose when they break.
/// </summary>
public sealed class MessagingPermissionsTests
{
    [Theory]
    [InlineData(AppRoles.Owner, AppRoles.Client, true)]
    [InlineData(AppRoles.Owner, AppRoles.CoHost, true)]
    [InlineData(AppRoles.CoHost, AppRoles.Owner, true)]
    [InlineData(AppRoles.CoHost, AppRoles.Client, true)]
    [InlineData(AppRoles.Manager, AppRoles.Client, true)]
    [InlineData(AppRoles.Client, AppRoles.Owner, true)]
    public void CanStartConversation_AllowedPairs_ReturnTrue(string senderRole, string recipientRole, bool expected)
    {
        var allowed = MessagingPermissions.CanStartConversation([senderRole], [recipientRole]);
        Assert.True(
            allowed == expected,
            $"Expected {senderRole} → {recipientRole} to be {(expected ? "allowed" : "blocked")} for starting a conversation, but got {allowed}.");
    }

    [Theory]
    [InlineData(AppRoles.Client, AppRoles.Client)]
    [InlineData(AppRoles.Client, AppRoles.CoHost)]
    [InlineData(AppRoles.CoHost, AppRoles.CoHost)]
    [InlineData(AppRoles.Manager, AppRoles.CoHost)]
    public void CanStartConversation_DisallowedPairs_ReturnFalse(string senderRole, string recipientRole)
    {
        var allowed = MessagingPermissions.CanStartConversation([senderRole], [recipientRole]);
        Assert.False(
            allowed,
            $"{senderRole} should not be allowed to start a conversation with {recipientRole}.");
    }

    [Theory]
    [InlineData(AppRoles.Owner, true)]
    [InlineData(AppRoles.CoHost, true)]
    [InlineData(AppRoles.Manager, true)]
    [InlineData(AppRoles.Client, true)]
    public void CanUseMessaging_KnownRoles_ReturnTrue(string role, bool expected)
    {
        var allowed = MessagingPermissions.CanUseMessaging([role]);
        Assert.True(
            allowed == expected,
            $"Role '{role}' messaging access did not match expectation (expected {expected}, got {allowed}).");
    }

    [Fact]
    public void CanBroadcast_OnlyOwnerMayBroadcast()
    {
        Assert.True(
            MessagingPermissions.CanBroadcast([AppRoles.Owner]),
            "Owner must be able to broadcast to multiple guests.");

        Assert.False(
            MessagingPermissions.CanBroadcast([AppRoles.CoHost]),
            "Co-hosts must not have broadcast permission.");
        Assert.False(
            MessagingPermissions.CanBroadcast([AppRoles.Client]),
            "Guests must not have broadcast permission.");
    }
}
