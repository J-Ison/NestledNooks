using NestledNooks.Data;

namespace NestledNooks.Models;

public sealed class PropertyEditModel
{
    public int Id { get; set; }

    public string Slug { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public bool IsPublished { get; set; } = true;

    public bool IsHomepage { get; set; }

    public int SortOrder { get; set; }

    public string MetaDescription { get; set; } = "";

    public string Subtitle { get; set; } = "";

    public List<string> Stats { get; set; } = [];

    public string TagsLine1 { get; set; } = "";

    public string TagsLine2 { get; set; } = "";

    public List<PropertyBadge> Badges { get; set; } = [];

    public string AboutText { get; set; } = "";

    public List<string> Amenities { get; set; } = [];

    public string LocationText { get; set; } = "";

    public string GuideTeaserText { get; set; } = "";

    public string BookingSubtext { get; set; } = "";

    public string BookingFinePrint { get; set; } = "";

    public string? AirbnbUrl { get; set; }

    public string? VrboUrl { get; set; }

    public List<PropertyPhoto> Photos { get; set; } = [];

    public static PropertyEditModel FromEntity(RentalProperty entity) => new()
    {
        Id = entity.Id,
        Slug = entity.Slug,
        DisplayName = entity.DisplayName,
        IsPublished = entity.IsPublished,
        IsHomepage = entity.IsHomepage,
        SortOrder = entity.SortOrder,
        MetaDescription = entity.MetaDescription,
        Subtitle = entity.Subtitle,
        Stats = PropertyContentJson.ParseStringList(entity.StatsJson).ToList(),
        TagsLine1 = entity.TagsLine1,
        TagsLine2 = entity.TagsLine2,
        Badges = PropertyContentJson.ParseBadges(entity.BadgesJson).ToList(),
        AboutText = entity.AboutText,
        Amenities = PropertyContentJson.ParseStringList(entity.AmenitiesJson).ToList(),
        LocationText = entity.LocationText,
        GuideTeaserText = entity.GuideTeaserText,
        BookingSubtext = entity.BookingSubtext,
        BookingFinePrint = entity.BookingFinePrint,
        AirbnbUrl = entity.AirbnbUrl,
        VrboUrl = entity.VrboUrl,
        Photos = PropertyContentJson.ParsePhotos(entity.PhotosJson).ToList(),
    };

    public RentalProperty ToEntity() => new()
    {
        Id = Id,
        Slug = Slug.Trim(),
        DisplayName = DisplayName.Trim(),
        IsPublished = IsPublished,
        IsHomepage = IsHomepage,
        SortOrder = SortOrder,
        MetaDescription = MetaDescription.Trim(),
        Subtitle = Subtitle.Trim(),
        StatsJson = PropertyContentJson.SerializeStringList(Stats),
        TagsLine1 = TagsLine1.Trim(),
        TagsLine2 = TagsLine2.Trim(),
        BadgesJson = PropertyContentJson.SerializeBadges(Badges),
        AboutText = AboutText.Trim(),
        AmenitiesJson = PropertyContentJson.SerializeStringList(Amenities),
        LocationText = LocationText.Trim(),
        GuideTeaserText = GuideTeaserText.Trim(),
        BookingSubtext = BookingSubtext.Trim(),
        BookingFinePrint = BookingFinePrint.Trim(),
        AirbnbUrl = string.IsNullOrWhiteSpace(AirbnbUrl) ? null : AirbnbUrl.Trim(),
        VrboUrl = string.IsNullOrWhiteSpace(VrboUrl) ? null : VrboUrl.Trim(),
        PhotosJson = PropertyContentJson.SerializePhotos(Photos),
        UpdatedAtUtc = DateTime.UtcNow,
    };
}
