using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NestledNooks.Data;
using NestledNooks.Models;

namespace NestledNooks.Services;

public sealed class BookingRequestService : IBookingRequestService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly IBookingAvailabilityService _availability;
    private readonly BookingPricingService _pricing;
    private readonly ILogger<BookingRequestService> _logger;

    public BookingRequestService(
        ApplicationDbContext db,
        IEmailService email,
        IBookingAvailabilityService availability,
        BookingPricingService pricing,
        ILogger<BookingRequestService> logger)
    {
        _db = db;
        _email = email;
        _availability = availability;
        _pricing = pricing;
        _logger = logger;
    }

    public async Task<BookingSubmitResult> SubmitAsync(
        BookingFormModel model,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var slug = model.PropertySlug.Trim().ToLowerInvariant();
        var property = _pricing.GetProperty(slug);
        if (property is null)
            return new BookingSubmitResult(false, null, null, "Unknown property.");

        if (model.CheckIn is not { } checkIn || model.CheckOut is not { } checkOut)
            return new BookingSubmitResult(false, null, null, "Check-in and check-out are required.");

        if (model.GuestCount > property.MaxGuests)
            return new BookingSubmitResult(false, null, null, $"Maximum {property.MaxGuests} guests.");

        if (model.PetCount > property.MaxPets)
            return new BookingSubmitResult(false, null, null, $"Maximum {property.MaxPets} pets.");

        BookingQuote quote;
        try
        {
            quote = await _pricing.CalculateAsync(slug, checkIn, checkOut, model.PetCount, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return new BookingSubmitResult(false, null, null, ex.Message);
        }

        if (!await _availability.IsRangeAvailableAsync(slug, checkIn, checkOut, cancellationToken: cancellationToken)
                .ConfigureAwait(false))
        {
            return new BookingSubmitResult(
                false,
                null,
                null,
                "Those dates are no longer available. Please choose different dates.");
        }

        var entity = new BookingRequest
        {
            UserId = userId,
            PropertySlug = slug,
            GuestFullName = model.GuestFullName.Trim(),
            GuestEmail = model.GuestEmail.Trim(),
            GuestPhone = string.IsNullOrWhiteSpace(model.GuestPhone) ? null : model.GuestPhone.Trim(),
            CheckIn = checkIn,
            CheckOut = checkOut,
            GuestCount = model.GuestCount,
            PetCount = model.PetCount,
            NightCount = quote.Nights,
            NightlyRate = quote.NightlyRate,
            CleaningFee = quote.CleaningFee,
            PetFee = quote.PetFee,
            Subtotal = quote.Subtotal,
            TotalAmount = quote.TotalAmount,
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
            Status = BookingStatuses.Pending,
            PaymentStatus = PaymentStatuses.Unpaid,
            AmountPaid = 0,
            PaymentReceivedAtUtc = null,
            CreatedAtUtc = DateTime.UtcNow,
            StatusUpdatedAtUtc = DateTime.UtcNow,
            // Unique index on BookingNumber; final value assigned after Id is generated.
            BookingNumber = Guid.NewGuid().ToString("N")
        };

        _db.BookingRequests.Add(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            entity.BookingNumber = $"NN-{DateTime.UtcNow:yyyyMMdd}-{entity.Id:D5}";
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            return new BookingSubmitResult(false, null, null, $"Could not save booking: {detail}");
        }

        var emailPayload = new BookingRequestEmailPayload(
            entity.Id,
            entity.BookingNumber,
            property.DisplayName,
            entity.GuestFullName,
            entity.GuestEmail,
            entity.GuestPhone,
            entity.CheckIn,
            entity.CheckOut,
            entity.GuestCount,
            entity.PetCount,
            entity.NightCount,
            entity.TotalAmount,
            entity.Notes);

        string? emailWarning = null;
        try
        {
            await _email.SendBookingRequestEmail(emailPayload).ConfigureAwait(false);
            await _email.SendBookingRequestGuestConfirmationEmail(emailPayload).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Booking {BookingNumber} saved but confirmation emails were not sent",
                entity.BookingNumber);
            emailWarning =
                "Your request was saved, but we could not send email (check SMTP settings). " +
                "We will still review your booking.";
        }

        return new BookingSubmitResult(true, entity.Id, entity.BookingNumber, null, emailWarning);
    }

    public async Task<BookingQuote?> GetQuoteAsync(
        string propertySlug,
        DateOnly checkIn,
        DateOnly checkOut,
        int petCount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _pricing.CalculateAsync(propertySlug, checkIn, checkOut, petCount, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<BookingRequest>> GetForUserAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        await _db.BookingRequests
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<BookingRequest>> GetPendingForPropertyAsync(
        string? propertySlug = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.BookingRequests
            .AsNoTracking()
            .Where(b => b.Status == BookingStatuses.Pending);

        if (!string.IsNullOrWhiteSpace(propertySlug))
            query = query.Where(b => b.PropertySlug == propertySlug);

        return await query
            .OrderBy(b => b.CheckIn)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BookingRequest>> GetAllForAdminAsync(
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.BookingRequests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(b => b.Status == status);

        return await query
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<BookingStatusUpdateResult> UpdatePaymentAsync(
        int bookingId,
        string paymentStatus,
        decimal? amountPaid,
        CancellationToken cancellationToken = default)
    {
        if (!IsAllowedPaymentStatus(paymentStatus))
            return new BookingStatusUpdateResult(false, "Invalid payment status.");

        var entity = await _db.BookingRequests
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
            return new BookingStatusUpdateResult(false, "Booking not found.");

        entity.PaymentStatus = paymentStatus;

        switch (paymentStatus)
        {
            case PaymentStatuses.Paid:
                entity.AmountPaid = amountPaid ?? entity.TotalAmount;
                entity.PaymentReceivedAtUtc = DateTime.UtcNow;
                break;
            case PaymentStatuses.PartiallyPaid:
                if (amountPaid is null or < 0)
                    return new BookingStatusUpdateResult(false, "Enter amount paid for partial payment.");
                entity.AmountPaid = amountPaid.Value;
                entity.PaymentReceivedAtUtc = DateTime.UtcNow;
                break;
            case PaymentStatuses.Unpaid:
                entity.AmountPaid = 0;
                entity.PaymentReceivedAtUtc = null;
                break;
            case PaymentStatuses.Refunded:
                entity.AmountPaid = amountPaid ?? entity.AmountPaid;
                entity.PaymentReceivedAtUtc ??= DateTime.UtcNow;
                break;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new BookingStatusUpdateResult(true, null);
    }

    public async Task<BookingStatusUpdateResult> UpdateStatusAsync(
        int bookingId,
        string newStatus,
        string? statusNote,
        CancellationToken cancellationToken = default)
    {
        if (!IsAllowedTransition(newStatus))
            return new BookingStatusUpdateResult(false, "Invalid status.");

        var entity = await _db.BookingRequests
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
            return new BookingStatusUpdateResult(false, "Booking not found.");

        var oldStatus = entity.Status;

        if (newStatus is BookingStatuses.Approved or BookingStatuses.Active)
        {
            if (!await _availability.IsRangeAvailableAsync(
                    entity.PropertySlug,
                    entity.CheckIn,
                    entity.CheckOut,
                    entity.Id,
                    cancellationToken).ConfigureAwait(false))
            {
                return new BookingStatusUpdateResult(
                    false,
                    "Cannot approve — dates conflict with another booking or external calendar.");
            }
        }

        entity.Status = newStatus;
        entity.StatusNote = string.IsNullOrWhiteSpace(statusNote) ? null : statusNote.Trim();
        entity.StatusUpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var property = _pricing.GetProperty(entity.PropertySlug);
        var displayName = property?.DisplayName ?? entity.PropertySlug;

        try
        {
            await _email.SendBookingStatusChangedEmailsAsync(new BookingStatusEmailPayload(
                entity.BookingNumber,
                displayName,
                entity.GuestFullName,
                entity.GuestEmail,
                entity.CheckIn,
                entity.CheckOut,
                entity.TotalAmount,
                oldStatus,
                newStatus,
                entity.StatusNote)).ConfigureAwait(false);
        }
        catch
        {
            // Status saved even if email fails
        }

        return new BookingStatusUpdateResult(true, null);
    }

    private static bool IsAllowedTransition(string status) =>
        status is BookingStatuses.Approved
            or BookingStatuses.Denied
            or BookingStatuses.Cancelled
            or BookingStatuses.Active
            or BookingStatuses.Ended;

    private static bool IsAllowedPaymentStatus(string status) =>
        status is PaymentStatuses.Unpaid
            or PaymentStatuses.PartiallyPaid
            or PaymentStatuses.Paid
            or PaymentStatuses.Refunded;
}
