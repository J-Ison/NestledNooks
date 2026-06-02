using NestledNooks.Data;
using NestledNooks.Models;

namespace NestledNooks.Services;

public interface IBookingRequestService
{
    Task<BookingSubmitResult> SubmitAsync(
        BookingFormModel model,
        string? userId,
        CancellationToken cancellationToken = default);

    Task<BookingQuote?> GetQuoteAsync(
        string propertySlug,
        DateOnly checkIn,
        DateOnly checkOut,
        int petCount);

    Task<IReadOnlyList<BookingRequest>> GetForUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingRequest>> GetPendingForPropertyAsync(
        string? propertySlug = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingRequest>> GetAllForAdminAsync(
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<BookingStatusUpdateResult> UpdatePaymentAsync(
        int bookingId,
        string paymentStatus,
        decimal? amountPaid,
        CancellationToken cancellationToken = default);

    Task<BookingStatusUpdateResult> UpdateStatusAsync(
        int bookingId,
        string newStatus,
        string? statusNote,
        CancellationToken cancellationToken = default);
}

public sealed record BookingSubmitResult(
    bool Succeeded,
    int? BookingId,
    string? BookingNumber,
    string? ErrorMessage,
    string? EmailWarning = null);

public sealed record BookingStatusUpdateResult(bool Succeeded, string? ErrorMessage);
