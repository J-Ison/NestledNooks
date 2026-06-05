using Microsoft.EntityFrameworkCore;
using NestledNooks.Data;

namespace NestledNooks.Services;

/// <summary>Idempotent SQL repairs when migration history and schema drift on production.</summary>
public static class DatabaseSchemaRepair
{
    public static async Task EnsureAllAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await EnsureAspNetUserProfileColumnsAsync(db, logger, cancellationToken).ConfigureAwait(false);
        await EnsureMessagingTablesAsync(db, logger, cancellationToken).ConfigureAwait(false);
        await EnsureContactInquiryTableAsync(db, logger, cancellationToken).ConfigureAwait(false);
        await EnsureSiteSettingsTableAsync(db, logger, cancellationToken).ConfigureAwait(false);
    }

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

    public static async Task EnsureMessagingTablesAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[MessageThreads]', N'U') IS NULL
            BEGIN
                CREATE TABLE [MessageThreads] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [CreatedAtUtc] datetime2 NOT NULL,
                    [UpdatedAtUtc] datetime2 NOT NULL,
                    CONSTRAINT [PK_MessageThreads] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_MessageThreads_UpdatedAtUtc] ON [MessageThreads] ([UpdatedAtUtc]);
            END

            IF OBJECT_ID(N'[MessageThreadParticipants]', N'U') IS NULL
            BEGIN
                CREATE TABLE [MessageThreadParticipants] (
                    [ThreadId] int NOT NULL,
                    [UserId] nvarchar(450) NOT NULL,
                    [LastReadAtUtc] datetime2 NULL,
                    CONSTRAINT [PK_MessageThreadParticipants] PRIMARY KEY ([ThreadId], [UserId]),
                    CONSTRAINT [FK_MessageThreadParticipants_MessageThreads_ThreadId]
                        FOREIGN KEY ([ThreadId]) REFERENCES [MessageThreads] ([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_MessageThreadParticipants_AspNetUsers_UserId]
                        FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_MessageThreadParticipants_UserId] ON [MessageThreadParticipants] ([UserId]);
            END

            IF OBJECT_ID(N'[Messages]', N'U') IS NULL
            BEGIN
                CREATE TABLE [Messages] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [ThreadId] int NOT NULL,
                    [SenderUserId] nvarchar(450) NOT NULL,
                    [Body] nvarchar(4000) NOT NULL,
                    [CreatedAtUtc] datetime2 NOT NULL,
                    CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Messages_MessageThreads_ThreadId]
                        FOREIGN KEY ([ThreadId]) REFERENCES [MessageThreads] ([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_Messages_AspNetUsers_SenderUserId]
                        FOREIGN KEY ([SenderUserId]) REFERENCES [AspNetUsers] ([Id])
                );
                CREATE INDEX [IX_Messages_SenderUserId] ON [Messages] ([SenderUserId]);
                CREATE INDEX [IX_Messages_ThreadId_CreatedAtUtc] ON [Messages] ([ThreadId], [CreatedAtUtc]);
            END
            """,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Verified messaging tables (MessageThreads, MessageThreadParticipants, Messages).");
    }

    public static async Task EnsureContactInquiryTableAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[ContactInquiries]', N'U') IS NULL
            BEGIN
                CREATE TABLE [ContactInquiries] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [SubmittedAtUtc] datetime2 NOT NULL,
                    [DisplayName] nvarchar(200) NOT NULL,
                    [ReplyEmail] nvarchar(256) NOT NULL,
                    [Message] nvarchar(4000) NOT NULL,
                    [SubmittedByUserId] nvarchar(450) NULL,
                    [IsVerifiedAccount] bit NOT NULL,
                    [Status] nvarchar(40) NOT NULL,
                    [ReadAtUtc] datetime2 NULL,
                    [OwnerNotes] nvarchar(2000) NULL,
                    CONSTRAINT [PK_ContactInquiries] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ContactInquiries_AspNetUsers_SubmittedByUserId]
                        FOREIGN KEY ([SubmittedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL
                );
                CREATE INDEX [IX_ContactInquiries_SubmittedAtUtc] ON [ContactInquiries] ([SubmittedAtUtc]);
                CREATE INDEX [IX_ContactInquiries_Status] ON [ContactInquiries] ([Status]);
            END
            """,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Verified ContactInquiries table.");
    }

    public static async Task EnsureSiteSettingsTableAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[SiteSettings]', N'U') IS NULL
            BEGIN
                CREATE TABLE [SiteSettings] (
                    [Id] int NOT NULL,
                    [MainQrCodeUrl] nvarchar(500) NULL,
                    [UpdatedAtUtc] datetime2 NOT NULL,
                    CONSTRAINT [PK_SiteSettings] PRIMARY KEY ([Id])
                );
            END
            """,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Verified SiteSettings table.");

        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('SiteSettings', 'DeerfieldGuestGuideQrCodeUrl') IS NULL
                ALTER TABLE [SiteSettings] ADD [DeerfieldGuestGuideQrCodeUrl] nvarchar(500) NULL;
            """,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Verified SiteSettings.DeerfieldGuestGuideQrCodeUrl column.");
    }
}
