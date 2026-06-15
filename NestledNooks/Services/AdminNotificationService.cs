using Microsoft.EntityFrameworkCore;
using NestledNooks.Data;

namespace NestledNooks.Services;

public sealed record AdminNotificationSummary(
    int UnreadMessages,
    int UnseenBookings,
    int NewContactInquiries,
    int NewUsers);

public sealed record BookingUnseenState(
    bool IsUnseen,
    string? UpdateLabel);

public interface IAdminNotificationService
{
    Task<AdminNotificationSummary> GetSummaryAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, BookingUnseenState>> GetBookingUnseenStatesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task MarkBookingSeenAsync(string userId, int bookingId, CancellationToken cancellationToken = default);

    Task MarkUsersSectionSeenAsync(string userId, CancellationToken cancellationToken = default);
}

public sealed class AdminNotificationService(
    ApplicationDbContext db,
    IMessagingService messaging,
    AdminNotificationStateNotifier notifier) : IAdminNotificationService
{
    public async Task<AdminNotificationSummary> GetSummaryAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var unreadMessages = await messaging.GetUnreadThreadCountAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        var unseenBookings = await CountUnseenBookingsAsync(userId, cancellationToken).ConfigureAwait(false);

        var newContactInquiries = await db.ContactInquiries
            .AsNoTracking()
            .CountAsync(i => i.Status == ContactInquiryStatuses.New, cancellationToken)
            .ConfigureAwait(false);

        var usersSeenAt = await db.AdminUserNotificationStates
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => s.UsersSectionSeenAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var seenThreshold = usersSeenAt ?? DateTime.MinValue;
        var newUsers = await db.Users
            .AsNoTracking()
            .CountAsync(
                u => u.Id != userId && u.RegisteredAtUtc > seenThreshold,
                cancellationToken)
            .ConfigureAwait(false);

        return new AdminNotificationSummary(
            unreadMessages,
            unseenBookings,
            newContactInquiries,
            newUsers);
    }

    public async Task<IReadOnlyDictionary<int, BookingUnseenState>> GetBookingUnseenStatesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var bookings = await db.BookingRequests
            .AsNoTracking()
            .Select(b => new
            {
                b.Id,
                b.Status,
                b.CreatedAtUtc,
                b.StatusUpdatedAtUtc,
                b.PaymentReceivedAtUtc,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var seen = await db.AdminBookingSeens
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToDictionaryAsync(s => s.BookingRequestId, s => s.SeenAtUtc, cancellationToken)
            .ConfigureAwait(false);

        var result = new Dictionary<int, BookingUnseenState>(bookings.Count);
        foreach (var booking in bookings)
        {
            DateTime? seenAt = seen.TryGetValue(booking.Id, out var seenValue) ? seenValue : null;
            var snapshot = new BookingRequest
            {
                Id = booking.Id,
                Status = booking.Status,
                CreatedAtUtc = booking.CreatedAtUtc,
                StatusUpdatedAtUtc = booking.StatusUpdatedAtUtc,
                PaymentReceivedAtUtc = booking.PaymentReceivedAtUtc,
            };

            var last = BookingRequestActivity.GetLastActivityUtc(snapshot);
            if (seenAt is not null && last <= seenAt)
            {
                result[booking.Id] = new BookingUnseenState(false, null);
                continue;
            }

            var label = BookingRequestActivity.DescribeUpdate(snapshot, seenAt);
            result[booking.Id] = new BookingUnseenState(true, label);
        }

        return result;
    }

    public async Task MarkBookingSeenAsync(
        string userId,
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        var row = await db.AdminBookingSeens
            .FirstOrDefaultAsync(s => s.UserId == userId && s.BookingRequestId == bookingId, cancellationToken)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        if (row is null)
        {
            db.AdminBookingSeens.Add(new AdminBookingSeen
            {
                UserId = userId,
                BookingRequestId = bookingId,
                SeenAtUtc = now,
            });
        }
        else
        {
            row.SeenAtUtc = now;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        notifier.NotifyChanged();
    }

    public async Task MarkUsersSectionSeenAsync(string userId, CancellationToken cancellationToken = default)
    {
        var row = await db.AdminUserNotificationStates
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        if (row is null)
        {
            db.AdminUserNotificationStates.Add(new AdminUserNotificationState
            {
                UserId = userId,
                UsersSectionSeenAtUtc = now,
            });
        }
        else
        {
            row.UsersSectionSeenAtUtc = now;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        notifier.NotifyChanged();
    }

    private async Task<int> CountUnseenBookingsAsync(string userId, CancellationToken cancellationToken)
    {
        var states = await GetBookingUnseenStatesAsync(userId, cancellationToken).ConfigureAwait(false);
        return states.Values.Count(s => s.IsUnseen);
    }
}
