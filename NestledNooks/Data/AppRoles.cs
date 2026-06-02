namespace NestledNooks.Data;

/// <summary>Application role names (AspNetRoles.Name). Use with [Authorize(Roles = ...)] and UserManager.</summary>
public static class AppRoles
{
    public const string Owner = "Owner";
    public const string CoHost = "CoHost";
    public const string Manager = "Manager";
    public const string Client = "Client";
}
