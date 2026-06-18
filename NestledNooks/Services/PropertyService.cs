using Microsoft.EntityFrameworkCore;
using NestledNooks.Data;

namespace NestledNooks.Services;

public interface IPropertyService
{
    Task<IReadOnlyList<RentalProperty>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RentalProperty>> GetPublishedAsync(CancellationToken cancellationToken = default);
    Task<RentalProperty?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<RentalProperty?> GetHomepageAsync(CancellationToken cancellationToken = default);
    Task<RentalProperty> SaveAsync(RentalProperty property, CancellationToken cancellationToken = default);
    Task<PropertyListingSettings> GetListingSettingsAsync(string slug, CancellationToken cancellationToken = default);
    Task EnsureSeededAsync(CancellationToken cancellationToken = default);
}

public sealed class PropertyService(ApplicationDbContext db) : IPropertyService
{
    public async Task<IReadOnlyList<RentalProperty>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.RentalProperties
            .AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.DisplayName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<RentalProperty>> GetPublishedAsync(CancellationToken cancellationToken = default) =>
        await db.RentalProperties
            .AsNoTracking()
            .Where(p => p.IsPublished)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.DisplayName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<RentalProperty?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        return await db.RentalProperties
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PropertyListingSettings> GetListingSettingsAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var property = await GetBySlugAsync(slug, cancellationToken).ConfigureAwait(false);
        return PropertyListingSettings.FromEntity(property);
    }

    public async Task<RentalProperty?> GetHomepageAsync(CancellationToken cancellationToken = default) =>
        await db.RentalProperties
            .AsNoTracking()
            .Where(p => p.IsPublished && p.IsHomepage)
            .OrderBy(p => p.SortOrder)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
        ?? await db.RentalProperties
            .AsNoTracking()
            .Where(p => p.IsPublished)
            .OrderBy(p => p.SortOrder)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<RentalProperty> SaveAsync(RentalProperty property, CancellationToken cancellationToken = default)
    {
        property.Slug = property.Slug.Trim().ToLowerInvariant();
        property.DisplayName = property.DisplayName.Trim();
        property.UpdatedAtUtc = DateTime.UtcNow;
        property.MinimumNights = ListingSettingsDefaults.ClampMinimumNights(property.MinimumNights);
        property.MinAdvanceBookingDays = ListingSettingsDefaults.ClampMinAdvanceBookingDays(property.MinAdvanceBookingDays);
        property.MaxBookingDaysAhead = ListingSettingsDefaults.ClampMaxBookingDaysAhead(property.MaxBookingDaysAhead);
        property.CleaningFee = ListingSettingsDefaults.ClampCleaningFee(property.CleaningFee);
        property.PetDepositPerTwoPets = ListingSettingsDefaults.ClampPetDepositPerTwoPets(property.PetDepositPerTwoPets);
        property.ExternalCalendarTrustDays = ListingSettingsDefaults.ClampExternalCalendarTrustDays(property.ExternalCalendarTrustDays);

        if (property.MinAdvanceBookingDays > property.MaxBookingDaysAhead)
            throw new InvalidOperationException("Earliest booking window cannot be later than the furthest booking window.");

        var effectiveMax = !property.AllowFarAdvanceDirectBooking && property.ExternalCalendarTrustDays > 0
            ? Math.Min(property.MaxBookingDaysAhead, property.ExternalCalendarTrustDays)
            : property.MaxBookingDaysAhead;

        if (property.MinAdvanceBookingDays > effectiveMax)
            throw new InvalidOperationException(
                "Minimum advance notice cannot be later than the furthest bookable check-in. " +
                "Raise the calendar trust window, enable far-ahead requests, or lower minimum advance notice.");

        if (property.IsHomepage)
        {
            var others = await db.RentalProperties
                .Where(p => p.IsHomepage && p.Id != property.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var other in others)
                other.IsHomepage = false;
        }

        var existing = await db.RentalProperties
            .FirstOrDefaultAsync(p => p.Id == property.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var slugTaken = await db.RentalProperties
                .AnyAsync(p => p.Slug == property.Slug, cancellationToken)
                .ConfigureAwait(false);
            if (slugTaken)
                throw new InvalidOperationException($"A property with slug '{property.Slug}' already exists.");

            db.RentalProperties.Add(property);
        }
        else
        {
            var slugTaken = await db.RentalProperties
                .AnyAsync(p => p.Slug == property.Slug && p.Id != property.Id, cancellationToken)
                .ConfigureAwait(false);
            if (slugTaken)
                throw new InvalidOperationException($"A property with slug '{property.Slug}' already exists.");

            if (LegalTextsChanged(existing, property))
                property.LegalDocumentsVersion = existing.LegalDocumentsVersion + 1;
            else
                property.LegalDocumentsVersion = existing.LegalDocumentsVersion;

            db.Entry(existing).CurrentValues.SetValues(property);
            property = existing;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return property;
    }

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        if (!await db.RentalProperties.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            db.RentalProperties.Add(PropertySeedData.CreateDeerfieldRetreat());
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var deerfield = await db.RentalProperties
            .FirstOrDefaultAsync(p => p.Slug == PropertySeedData.DeerfieldSlug, cancellationToken)
            .ConfigureAwait(false);

        if (deerfield is not null &&
            string.Equals(deerfield.GuideTeaserText, PropertySeedData.LegacyGuideTeaserText, StringComparison.Ordinal))
        {
            deerfield.GuideTeaserText = PropertySeedData.DefaultGuideTeaserText;
            deerfield.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (deerfield is not null && string.IsNullOrWhiteSpace(deerfield.RentalAgreementText))
        {
            var name = deerfield.DisplayName;
            deerfield.RentalAgreementText = PropertyLegalDefaults.RentalAgreement(name);
            deerfield.HouseRulesText = PropertyLegalDefaults.HouseRules(name);
            deerfield.LiabilityAcknowledgmentText = PropertyLegalDefaults.LiabilityAcknowledgment(name);
            deerfield.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool LegalTextsChanged(RentalProperty existing, RentalProperty updated) =>
        !string.Equals(existing.RentalAgreementText, updated.RentalAgreementText, StringComparison.Ordinal) ||
        !string.Equals(existing.HouseRulesText, updated.HouseRulesText, StringComparison.Ordinal) ||
        !string.Equals(existing.LiabilityAcknowledgmentText, updated.LiabilityAcknowledgmentText, StringComparison.Ordinal);
}
