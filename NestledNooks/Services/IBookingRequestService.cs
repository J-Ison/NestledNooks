using NestledNooks.Data;
using NestledNooks.Models;

namespace NestledNooks.Services;

public interface IBookingRequestService
{
    Task<BookingSubmitResult> SubmitAsync(
        BookingFormModel model,
        string? userId,
        CancellationToken cancellationToken = default);

    Task<BookingConfirmationSummary?> GetPublicConfirmationAsync(
        string bookingNumber,
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

    Task<BookingPaymentReviewSnapshot?> GetPaymentReviewAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<BookingPaymentCheckoutResult> ProceedToPaymentCheckoutAsync(
        string token,
        string siteBaseUrl,
        bool agreedToRentalAgreement,
        bool agreedToHouseRules,
        bool agreedToLiabilityAcknowledgment,
        CancellationToken cancellationToken = default);
}

public sealed record BookingSubmitResult(
    bool Succeeded,
    int? BookingId,
    string? BookingNumber,
    string? ErrorCode,
    string? ErrorMessage,
    string? EmailWarning = null);

public sealed record BookingConfirmationSummary(
    string BookingNumber,
    string PropertySlug,
    string PropertyDisplayName,
    string GuestFullName,
    DateOnly CheckIn,
    DateOnly CheckOut,
    string CheckInTimeDisplay,
    string CheckOutTimeDisplay,
    int NightCount,
    decimal TotalAmount,
    string Status);

public sealed record BookingStatusUpdateResult(bool Succeeded, string? ErrorMessage);

public sealed record BookingApprovalResult(
    bool Succeeded,
    string? ErrorMessage,
    string? PaymentUrl,
    string? EmailWarning = null);

public sealed record BookingPaymentReviewSnapshot(
    string Token,
    string BookingNumber,
    string PropertySlug,
    string PropertyDisplayName,
    DateOnly CheckIn,
    DateOnly CheckOut,
    decimal Amount,
    string Purpose,
    string PurposeLabel,
    PropertyLegalSnapshot LegalDocuments);

public sealed record BookingPaymentCheckoutResult(
    bool Succeeded,
    string? CheckoutUrl,
    string? ErrorMessage);
