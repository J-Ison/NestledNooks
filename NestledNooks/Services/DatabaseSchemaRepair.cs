using Microsoft.EntityFrameworkCore;
using NestledNooks.Data;

namespace NestledNooks.Services;

/// <summary>
/// Idempotent SQL repairs for production when migration history and schema drift.</summary>
public static class DatabaseSchemaRepair
{
    public static async Task EnsureAspNetUserProfileColumnsAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('AspNetUsers', 'Nickname') IS NULL
                ALTER TABLE [AspNetUsers] ADD [Nickname] nvarchar(50) NULL;

            IF COL_LENGTH('AspNetUsers', 'MessageTagsJson') IS NULL
                ALTER TABLE [AspNetUsers] ADD [MessageTagsJson] nvarchar(500) NULL;
            """,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Verified AspNetUsers profile columns (Nickname, MessageTagsJson).");
    }
}
