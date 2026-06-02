using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace NestledNooks.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<BookingRequest> BookingRequests => Set<BookingRequest>();
    public DbSet<ExternalCalendarEvent> ExternalCalendarEvents => Set<ExternalCalendarEvent>();

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
    }
}
