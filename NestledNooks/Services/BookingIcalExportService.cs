using System.Text;
using Microsoft.EntityFrameworkCore;
using NestledNooks.Data;

namespace NestledNooks.Services;

/// <summary>
/// Builds an iCal feed of site holds (Pending/Approved) for import into Airbnb/Vrbo calendar sync.
/// </summary>
public sealed class BookingIcalExportService
{
    private readonly ApplicationDbContext _db;

    public BookingIcalExportService(ApplicationDbContext db) => _db = db;

    public async Task<string> BuildHoldsFeedAsync(string propertySlug, CancellationToken cancellationToken = default)
    {
        var holds = await _db.BookingRequests
            .AsNoTracking()
            .Where(b => b.PropertySlug == propertySlug && BookingStatuses.DateHolding.Contains(b.Status))
            .OrderBy(b => b.CheckIn)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//Nestled Nooks//Booking Holds//EN");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");
        sb.AppendLine("X-WR-CALNAME:Nestled Nooks Holds");

        foreach (var b in holds)
        {
            var uid = $"nn-booking-{b.Id}@nestlednooks";
            var stamp = b.CreatedAtUtc.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{uid}");
            sb.AppendLine($"DTSTAMP:{stamp}");
            sb.AppendLine($"DTSTART;VALUE=DATE:{b.CheckIn:yyyyMMdd}");
            sb.AppendLine($"DTEND;VALUE=DATE:{b.CheckOut:yyyyMMdd}");
            sb.AppendLine($"SUMMARY:NN {b.Status} {b.BookingNumber}");
            sb.AppendLine("TRANSP:OPAQUE");
            sb.AppendLine("END:VEVENT");
        }

        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }
}
