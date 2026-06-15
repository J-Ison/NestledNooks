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
        int petCount,
        CancellationToken cancellationToken = default);

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

    Task<BookingApprovalResult> ApproveForFullPaymentAsync(
        int bookingId,
        string? statusNote,
        string siteBaseUrl,
        CancellationToken cancellationToken = default);

    Task<BookingApprovalResult> ApproveWithDepositAsync(
        int bookingId,
        decimal depositAmount,
        string? statusNote,
        string siteBaseUrl,
        CancellationToken cancellationToken = default);

    Task<BookingPaymentLinkResult> CreateBalancePaymentLinkAsync(
        int bookingId,
        string siteBaseUrl,
        CancellationToken cancellationToken = default);

    Task<BookingStatusUpdateResult> SendGuestEmailAsync(
        int bookingId,
        string message,
        string? emailSubject = null,
        CancellationToken cancellationToken = default);

    Task<BookingStatusUpdateResult> SyncPaymentFromStripeAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<string?> GetLatestPaymentUrlAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, string>> GetLatestPaymentUrlsAsync(
        IEnumerable<int> bookingIds,
        CancellationToken cancellationToken = default);
}

public sealed record BookingSubmitResult(
    bool Succeeded,
    int? BookingId,
    string? BookingNumber,
    string? ErrorMessage,
    string? EmailWarning = null);

public sealed record BookingStatusUpdateResult(bool Succeeded, string? ErrorMessage);

public sealed record BookingApprovalResult(
    bool Succeeded,
    string? ErrorMessage,
    string? PaymentUrl,
    string? EmailWarning = null);
