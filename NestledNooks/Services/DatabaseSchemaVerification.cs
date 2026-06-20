using Microsoft.EntityFrameworkCore;
using NestledNooks.Data;

namespace NestledNooks.Services;

/// <summary>Post-migration sanity checks for production deploys.</summary>
public static class DatabaseSchemaVerification
{
    public static async Task VerifyAndLogAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var checks = new (string Name, string Sql)[]
        {
            ("PropertyNightlyRates", "SELECT CASE WHEN OBJECT_ID(N'[PropertyNightlyRates]', N'U') IS NOT NULL THEN 1 ELSE 0 END"),
            ("BookingPaymentLinks", "SELECT CASE WHEN OBJECT_ID(N'[BookingPaymentLinks]', N'U') IS NOT NULL THEN 1 ELSE 0 END"),
            ("GuestEmailTemplates", "SELECT CASE WHEN OBJECT_ID(N'[GuestEmailTemplates]', N'U') IS NOT NULL THEN 1 ELSE 0 END"),
            ("AdminBookingSeens", "SELECT CASE WHEN OBJECT_ID(N'[AdminBookingSeens]', N'U') IS NOT NULL THEN 1 ELSE 0 END"),
            ("SiteSettings.DirectBookingsEnabled", "SELECT CASE WHEN COL_LENGTH('SiteSettings', 'DirectBookingsEnabled') IS NOT NULL THEN 1 ELSE 0 END"),
            ("BookingRequests.PaymentStatus", "SELECT CASE WHEN COL_LENGTH('BookingRequests', 'PaymentStatus') IS NOT NULL THEN 1 ELSE 0 END"),
            ("AspNetUsers.RegisteredAtUtc", "SELECT CASE WHEN COL_LENGTH('AspNetUsers', 'RegisteredAtUtc') IS NOT NULL THEN 1 ELSE 0 END"),
            ("RentalProperties.DiscountsJson", "SELECT CASE WHEN COL_LENGTH('RentalProperties', 'DiscountsJson') IS NOT NULL THEN 1 ELSE 0 END"),
            ("RentalProperties.ShowChannelPriceLinks", "SELECT CASE WHEN COL_LENGTH('RentalProperties', 'ShowChannelPriceLinks') IS NOT NULL THEN 1 ELSE 0 END"),
            ("RentalProperties.RentalAgreementText", "SELECT CASE WHEN COL_LENGTH('RentalProperties', 'RentalAgreementText') IS NOT NULL THEN 1 ELSE 0 END"),
        };

        var failures = new List<string>();
        foreach (var (name, sql) in checks)
        {
            var exists = await db.Database
                .SqlQueryRaw<int>(sql)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (exists == 1)
                logger.LogInformation("Schema check OK: {Check}", name);
            else
                failures.Add(name);
        }

        if (failures.Count > 0)
        {
            logger.LogError(
                "Schema verification failed for: {Checks}. Review migration history and DatabaseSchemaRepair logs.",
                string.Join(", ", failures));
        }
        else
        {
            logger.LogInformation("All critical schema checks passed.");
        }
    }
}
