using System.Security.Claims;
using NestledNooks.Data;

namespace NestledNooks.Services;

public static class HostStaffAuthorization
{
    public static bool IsHostStaff(ClaimsPrincipal? user) =>
        user?.Identity?.IsAuthenticated == true &&
        (user.IsInRole(AppRoles.Owner) ||
         user.IsInRole(AppRoles.CoHost) ||
         user.IsInRole(AppRoles.Manager));

    public static bool IsOwner(ClaimsPrincipal? user) =>
        user?.Identity?.IsAuthenticated == true && user.IsInRole(AppRoles.Owner);

    public static void EnsureHostStaff(ClaimsPrincipal? user)
    {
        if (!IsHostStaff(user))
            throw new UnauthorizedAccessException("Host staff access is required.");
    }
}
