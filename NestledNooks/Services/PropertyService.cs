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
    }
}
