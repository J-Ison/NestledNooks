using Microsoft.AspNetCore.Mvc;
using NestledNooks.Models;
using NestledNooks.Services;

namespace NestledNooks.Controllers;

[ApiController]
[Route("api/calendar")]
public class CalendarController : ControllerBase
{
    private readonly BookingIcalExportService _export;

    public CalendarController(BookingIcalExportService export) => _export = export;

    /// <summary>
    /// Import this URL into Airbnb/Vrbo as a secondary calendar to block Pending/Approved site requests.
    /// </summary>
    [HttpGet("{propertySlug}/holds.ics")]
    [Produces("text/calendar")]
    public async Task<IActionResult> GetHoldsFeed(string propertySlug, CancellationToken cancellationToken)
    {
        if (!propertySlug.Equals(BookingFormModel.DeerfieldSlug, StringComparison.OrdinalIgnoreCase))
            return NotFound();

        var ical = await _export.BuildHoldsFeedAsync(propertySlug, cancellationToken).ConfigureAwait(false);
        return Content(ical, "text/calendar", System.Text.Encoding.UTF8);
    }
}
