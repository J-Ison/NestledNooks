using NestledNooks.Data;

namespace NestledNooks.Services;

public static class MessagingPermissions
{
    public static bool CanStartConversation(IEnumerable<string> senderRoles, IEnumerable<string> recipientRoles)
    {
        if (senderRoles.Contains(AppRoles.Owner))
            return true;

        if (senderRoles.Contains(AppRoles.CoHost) || senderRoles.Contains(AppRoles.Manager))
            return recipientRoles.Contains(AppRoles.Owner) || recipientRoles.Contains(AppRoles.Client);

        if (senderRoles.Contains(AppRoles.Client))
            return recipientRoles.Contains(AppRoles.Owner);

        return false;
    }

    public static bool CanUseMessaging(IEnumerable<string> roles) =>
        roles.Contains(AppRoles.Owner)
        || roles.Contains(AppRoles.CoHost)
        || roles.Contains(AppRoles.Manager)
        || roles.Contains(AppRoles.Client);

    public static bool CanBroadcast(IEnumerable<string> roles) =>
        roles.Contains(AppRoles.Owner);
}
