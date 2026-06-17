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
        await EnsureBookingRequestPaymentColumnsAsync(db, logger, cancellationToken).ConfigureAwait(false);
        await EnsureMessagingTablesAsync(db, logger, cancellationToken).ConfigureAwait(false);
        await EnsureContactInquiryTableAsync(db, logger, cancellationToken).ConfigureAwait(false);
        await EnsureSiteSettingsTableAsync(db, logger, cancellationToken).ConfigureAwait(false);
        await EnsureRentalPropertyCleaningFeeColumnAsync(db, logger, cancellationToken).ConfigureAwait(false);
        await EnsureRentalPropertyListingSettingsColumnsAsync(db, logger, cancellationToken).ConfigureAwait(false);
        await EnsurePropertyNightlyRatesTableAsync(db, logger, cancellationToken).ConfigureAwait(false);
        await EnsureStripeBookingPaymentSchemaAsync(db, logger, cancellationToken).ConfigureAwait(false);
        await EnsureGuestEmailTemplatesTableAsync(db, logger, cancellationToken).ConfigureAwait(false);
        await EnsureAdminNotificationSchemaAsync(db, logger, cancellationToken).ConfigureAwait(false);
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

    public static async Task EnsureBookingRequestPaymentColumnsAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('BookingRequests', 'PaymentStatus') IS NULL
                ALTER TABLE [BookingRequests] ADD [PaymentStatus] nvarchar(40) NOT NULL
                    CONSTRAINT [DF_BookingRequests_PaymentStatus] DEFAULT ('Unpaid');

            IF COL_LENGTH('BookingRequests', 'AmountPaid') IS NULL
                ALTER TABLE [BookingRequests] ADD [AmountPaid] decimal(18,2) NOT NULL
                    CONSTRAINT [DF_BookingRequests_AmountPaid] DEFAULT (0);

            IF COL_LENGTH('BookingRequests', 'PaymentReceivedAtUtc') IS NULL
                ALTER TABLE [BookingRequests] ADD [PaymentReceivedAtUtc] datetime2 NULL;

            IF COL_LENGTH('BookingRequests', 'PaymentStatus') IS NOT NULL
                UPDATE [BookingRequests]
                SET [PaymentStatus] = 'Unpaid'
                WHERE [PaymentStatus] IS NULL OR [PaymentStatus] = '';
            """,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Verified BookingRequests payment columns.");
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

        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('SiteSettings', 'GuestEmailHeaderTemplate') IS NULL
                ALTER TABLE [SiteSettings] ADD [GuestEmailHeaderTemplate] nvarchar(2000) NULL;

            IF COL_LENGTH('SiteSettings', 'GuestEmailFooterTemplate') IS NULL
                ALTER TABLE [SiteSettings] ADD [GuestEmailFooterTemplate] nvarchar(4000) NULL;
            """,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Verified SiteSettings guest email wrapper columns.");

        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('SiteSettings', 'DirectBookingsEnabled') IS NULL
                ALTER TABLE [SiteSettings] ADD [DirectBookingsEnabled] bit NOT NULL
                    CONSTRAINT [DF_SiteSettings_DirectBookingsEnabled] DEFAULT (1);
            """,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Verified SiteSettings.DirectBookingsEnabled column.");

        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('SiteSettings', 'MinimumNights') IS NULL
                ALTER TABLE [SiteSettings] ADD [MinimumNights] int NOT NULL
                    CONSTRAINT [DF_SiteSettings_MinimumNights] DEFAULT (2);

            IF COL_LENGTH('SiteSettings', 'MinAdvanceBookingDays') IS NULL
                ALTER TABLE [SiteSettings] ADD [MinAdvanceBookingDays] int NOT NULL
                    CONSTRAINT [DF_SiteSettings_MinAdvanceBookingDays] DEFAULT (10);

            IF COL_LENGTH('SiteSettings', 'MaxBookingDaysAhead') IS NULL
                ALTER TABLE [SiteSettings] ADD [MaxBookingDaysAhead] int NOT NULL
                    CONSTRAINT [DF_SiteSettings_MaxBookingDaysAhead] DEFAULT (365);

            IF COL_LENGTH('SiteSettings', 'CleaningFee') IS NULL
                ALTER TABLE [SiteSettings] ADD [CleaningFee] decimal(18,2) NOT NULL
                    CONSTRAINT [DF_SiteSettings_CleaningFee] DEFAULT (200);

            IF COL_LENGTH('SiteSettings', 'PetDepositPerTwoPets') IS NULL
                ALTER TABLE [SiteSettings] ADD [PetDepositPerTwoPets] decimal(18,2) NOT NULL
                    CONSTRAINT [DF_SiteSettings_PetDepositPerTwoPets] DEFAULT (50);
            """,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Verified SiteSettings listing booking columns.");

        await db.Database.ExecuteSqlRawAsync(
            """
            IF NOT EXISTS (SELECT 1 FROM [SiteSettings] WHERE [Id] = 1)
            BEGIN
                INSERT INTO [SiteSettings] ([Id], [UpdatedAtUtc], [DirectBookingsEnabled])
                VALUES (1, SYSUTCDATETIME(), 1);
            END
            """,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Verified SiteSettings default row (Id = 1).");
    }

    public static async Task EnsureRentalPropertyCleaningFeeColumnAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('RentalProperties', 'CleaningFee') IS NULL
                ALTER TABLE [RentalProperties] ADD [CleaningFee] decimal(18,2) NOT NULL CONSTRAINT [DF_RentalProperties_CleaningFee] DEFAULT (150);
            """,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Verified RentalProperties.CleaningFee column.");
    }

    public static async Task EnsureRentalPropertyListingSettingsColumnsAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('RentalProperties', 'MinimumNights') IS NULL
                ALTER TABLE [RentalProperties] ADD [MinimumNights] int NOT NULL
                    CONSTRAINT [DF_RentalProperties_MinimumNights] DEFAULT (2);

            IF COL_LENGTH('RentalProperties', 'MinAdvanceBookingDays') IS NULL
                ALTER TABLE [RentalProperties] ADD [MinAdvanceBookingDays] int NOT NULL
                    CONSTRAINT [DF_RentalProperties_MinAdvanceBookingDays] DEFAULT (10);

            IF COL_LENGTH('RentalProperties', 'MaxBookingDaysAhead') IS NULL
                ALTER TABLE [RentalProperties] ADD [MaxBookingDaysAhead] int NOT NULL
                    CONSTRAINT [DF_RentalProperties_MaxBookingDaysAhead] DEFAULT (365);

            IF COL_LENGTH('RentalProperties', 'PetDepositPerTwoPets') IS NULL
                ALTER TABLE [RentalProperties] ADD [PetDepositPerTwoPets] decimal(18,2) NOT NULL
                    CONSTRAINT [DF_RentalProperties_PetDepositPerTwoPets] DEFAULT (50);

            IF COL_LENGTH('RentalProperties', 'ExternalCalendarTrustDays') IS NULL
                ALTER TABLE [RentalProperties] ADD [ExternalCalendarTrustDays] int NOT NULL
                    CONSTRAINT [DF_RentalProperties_ExternalCalendarTrustDays] DEFAULT (180);

            IF COL_LENGTH('RentalProperties', 'AllowFarAdvanceDirectBooking') IS NULL
                ALTER TABLE [RentalProperties] ADD [AllowFarAdvanceDirectBooking] bit NOT NULL
                    CONSTRAINT [DF_RentalProperties_AllowFarAdvanceDirectBooking] DEFAULT (1);
            """,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Verified RentalProperties listing booking columns.");
    }

    public static async Task EnsurePropertyNightlyRatesTableAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[PropertyNightlyRates]', N'U') IS NULL
            BEGIN
                CREATE TABLE [PropertyNightlyRates] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [PropertySlug] nvarchar(120) NOT NULL,
                    [Date] date NOT NULL,
                    [Rate] decimal(18,2) NOT NULL,
                    [MinimumStay] int NULL,
                    [UpdatedAtUtc] datetime2 NOT NULL,
                    CONSTRAINT [PK_PropertyNightlyRates] PRIMARY KEY ([Id])
                );
                CREATE UNIQUE INDEX [IX_PropertyNightlyRates_PropertySlug_Date]
                    ON [PropertyNightlyRates] ([PropertySlug], [Date]);
            END
            """,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Verified PropertyNightlyRates table.");
    }

    public static async Task EnsureStripeBookingPaymentSchemaAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('BookingRequests', 'RequiredDepositAmount') IS NULL
                ALTER TABLE [BookingRequests] ADD [RequiredDepositAmount] decimal(18,2) NULL;

            IF COL_LENGTH('BookingRequests', 'DepositNonRefundable') IS NULL
                ALTER TABLE [BookingRequests] ADD [DepositNonRefundable] bit NOT NULL CONSTRAINT [DF_BookingRequests_DepositNonRefundable] DEFAULT (0);

            IF OBJECT_ID(N'[BookingPaymentLinks]', N'U') IS NULL
            BEGIN
                CREATE TABLE [BookingPaymentLinks] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [BookingRequestId] int NOT NULL,
                    [Token] nvarchar(64) NOT NULL,
                    [Purpose] nvarchar(20) NOT NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [StripeCheckoutSessionId] nvarchar(200) NULL,
                    [CreatedAtUtc] datetime2 NOT NULL,
                    [CompletedAtUtc] datetime2 NULL,
                    CONSTRAINT [PK_BookingPaymentLinks] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_BookingPaymentLinks_BookingRequests_BookingRequestId]
                        FOREIGN KEY ([BookingRequestId]) REFERENCES [BookingRequests] ([Id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_BookingPaymentLinks_BookingRequestId] ON [BookingPaymentLinks] ([BookingRequestId]);
                CREATE UNIQUE INDEX [IX_BookingPaymentLinks_Token] ON [BookingPaymentLinks] ([Token]);
            END
            """,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Verified Stripe booking payment schema (deposit columns, BookingPaymentLinks).");
    }

    public static async Task EnsureGuestEmailTemplatesTableAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[GuestEmailTemplates]', N'U') IS NULL
            BEGIN
                CREATE TABLE [GuestEmailTemplates] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [PropertySlug] nvarchar(120) NOT NULL,
                    [Category] nvarchar(40) NOT NULL,
                    [Title] nvarchar(120) NOT NULL,
                    [EmailSubject] nvarchar(200) NULL,
                    [Body] nvarchar(max) NOT NULL,
                    [SortOrder] int NOT NULL,
                    [UpdatedAtUtc] datetime2 NOT NULL,
                    CONSTRAINT [PK_GuestEmailTemplates] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_GuestEmailTemplates_PropertySlug_SortOrder] ON [GuestEmailTemplates] ([PropertySlug], [SortOrder]);
            END
            """,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Verified GuestEmailTemplates table.");
    }

    public static async Task EnsureAdminNotificationSchemaAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            IF COL_LENGTH('AspNetUsers', 'RegisteredAtUtc') IS NULL
                ALTER TABLE [AspNetUsers] ADD [RegisteredAtUtc] datetime2 NOT NULL
                    CONSTRAINT [DF_AspNetUsers_RegisteredAtUtc] DEFAULT ('2000-01-01T00:00:00');

            IF OBJECT_ID(N'[AdminBookingSeens]', N'U') IS NULL
            BEGIN
                CREATE TABLE [AdminBookingSeens] (
                    [UserId] nvarchar(450) NOT NULL,
                    [BookingRequestId] int NOT NULL,
                    [SeenAtUtc] datetime2 NOT NULL,
                    CONSTRAINT [PK_AdminBookingSeens] PRIMARY KEY ([UserId], [BookingRequestId]),
                    CONSTRAINT [FK_AdminBookingSeens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_AdminBookingSeens_BookingRequests_BookingRequestId] FOREIGN KEY ([BookingRequestId]) REFERENCES [BookingRequests] ([Id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_AdminBookingSeens_BookingRequestId] ON [AdminBookingSeens] ([BookingRequestId]);
            END

            IF OBJECT_ID(N'[AdminUserNotificationStates]', N'U') IS NULL
            BEGIN
                CREATE TABLE [AdminUserNotificationStates] (
                    [UserId] nvarchar(450) NOT NULL,
                    [UsersSectionSeenAtUtc] datetime2 NULL,
                    CONSTRAINT [PK_AdminUserNotificationStates] PRIMARY KEY ([UserId]),
                    CONSTRAINT [FK_AdminUserNotificationStates_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                );
            END
            """,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Verified admin notification schema.");
    }
}
