using Microsoft.AspNetCore.Http;

namespace NestledNooks.Services;

public static class ClientIpResolver
{
    public static string? GetClientIp(HttpContext? context)
    {
        if (context is null)
            return null;

        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
                return first;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
