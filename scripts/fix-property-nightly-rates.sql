-- Run in Azure SQL Query editor if booking quotes fail with:
-- Invalid object name 'PropertyNightlyRates'.

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

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612210006_AddPropertyNightlyRates'
)
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260612210006_AddPropertyNightlyRates', N'8.0.0');

SELECT COUNT(*) AS PropertyNightlyRateRows FROM [PropertyNightlyRates];
