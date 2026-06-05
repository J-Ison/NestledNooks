using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace NestledNooks.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<BookingRequest> BookingRequests => Set<BookingRequest>();
    public DbSet<ExternalCalendarEvent> ExternalCalendarEvents => Set<ExternalCalendarEvent>();
    public DbSet<SiteTheme> SiteThemes => Set<SiteTheme>();
    public DbSet<RentalProperty> RentalProperties => Set<RentalProperty>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<BookingRequest>(e =>
        {
            e.Property(x => x.BookingNumber).HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.BookingNumber).IsUnique();
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => new { x.PropertySlug, x.CheckIn, x.CheckOut });
            e.HasIndex(x => x.Status);

            e.Property(x => x.PropertySlug).HasMaxLength(120).IsRequired();
            e.Property(x => x.GuestFullName).HasMaxLength(200).IsRequired();
            e.Property(x => x.GuestEmail).HasMaxLength(256).IsRequired();
            e.Property(x => x.GuestPhone).HasMaxLength(40);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.Status).HasMaxLength(40).IsRequired();
            e.Property(x => x.StatusNote).HasMaxLength(500);
            e.Property(x => x.PaymentStatus).HasMaxLength(40).IsRequired();
            e.Property(x => x.AmountPaid).HasPrecision(18, 2);

            e.Property(x => x.NightlyRate).HasPrecision(18, 2);
            e.Property(x => x.CleaningFee).HasPrecision(18, 2);
            e.Property(x => x.PetFee).HasPrecision(18, 2);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);

            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.CreatedAtUtc);
        });

        builder.Entity<ExternalCalendarEvent>(e =>
        {
            e.Property(x => x.PropertySlug).HasMaxLength(120).IsRequired();
            e.Property(x => x.Source).HasMaxLength(40).IsRequired();
            e.Property(x => x.Summary).HasMaxLength(500);
            e.HasIndex(x => new { x.PropertySlug, x.StartDate, x.EndDate });
        });

        builder.Entity<SiteTheme>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.PresetKey).HasMaxLength(40).IsRequired();
            e.Property(x => x.PrimaryColor).HasMaxLength(16).IsRequired();
            e.Property(x => x.PrimaryLightColor).HasMaxLength(16).IsRequired();
            e.Property(x => x.PrimarySoftBg).HasMaxLength(16).IsRequired();
            e.Property(x => x.PrimaryBorderColor).HasMaxLength(16).IsRequired();
            e.Property(x => x.PrimaryTextColor).HasMaxLength(16).IsRequired();
            e.Property(x => x.AccentColor).HasMaxLength(16).IsRequired();
            e.Property(x => x.AccentBorderColor).HasMaxLength(16).IsRequired();
            e.Property(x => x.HeroStartColor).HasMaxLength(16).IsRequired();
            e.Property(x => x.HeroMidColor).HasMaxLength(16).IsRequired();
            e.Property(x => x.HeroEndColor).HasMaxLength(16).IsRequired();
            e.Property(x => x.HeroBorderColor).HasMaxLength(16).IsRequired();
            e.Property(x => x.BookingColor).HasMaxLength(16).IsRequired();
            e.Property(x => x.BookingDarkColor).HasMaxLength(16).IsRequired();
            e.Property(x => x.PageBgTop).HasMaxLength(16).IsRequired();
            e.Property(x => x.PageBgBottom).HasMaxLength(16).IsRequired();
        });

        builder.Entity<RentalProperty>(e =>
        {
            e.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            e.Property(x => x.MetaDescription).HasMaxLength(500);
            e.Property(x => x.Subtitle).HasMaxLength(1000);
            e.Property(x => x.TagsLine1).HasMaxLength(300);
            e.Property(x => x.TagsLine2).HasMaxLength(300);
            e.Property(x => x.BookingSubtext).HasMaxLength(500);
            e.Property(x => x.BookingFinePrint).HasMaxLength(500);
            e.Property(x => x.AirbnbUrl).HasMaxLength(500);
            e.Property(x => x.VrboUrl).HasMaxLength(500);
        });
    }
}
