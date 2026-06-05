using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NestledNooks.Data;

namespace NestledNooks.Services;

public sealed class MessagingService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    IOptions<AdminOptions> adminOptions) : IMessagingService
{
    private const int MaxBodyLength = 4000;

    private IReadOnlyList<string> OwnerEmails { get; } = adminOptions.Value.OwnerEmails
        .Where(e => !string.IsNullOrWhiteSpace(e))
        .Select(e => e.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    public async Task<IReadOnlyList<MessageThreadSummary>> GetInboxAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var participantRows = await db.MessageThreadParticipants
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new
            {
                p.ThreadId,
                p.LastReadAtUtc,
                ThreadUpdatedAtUtc = p.Thread.UpdatedAtUtc,
                ParticipantCount = p.Thread.Participants.Count,
                OtherParticipants = p.Thread.Participants
                    .Where(op => op.UserId != userId)
                    .Select(op => new
                    {
                        op.UserId,
                        op.User.Email,
                        op.User.UserName,
                        op.User.Nickname,
                        op.User.MessageTagsJson,
                    })
                    .ToList(),
                LatestMessage = p.Thread.Messages
                    .OrderByDescending(m => m.CreatedAtUtc)
                    .Select(m => new { m.Body, m.CreatedAtUtc, m.SenderUserId })
                    .FirstOrDefault(),
            })
            .OrderByDescending(x => x.ThreadUpdatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var otherUserIds = participantRows
            .SelectMany(r => r.OtherParticipants.Select(p => p.UserId))
            .Distinct()
            .ToList();
        var ownerUserIds = await GetOwnerUserIdsAsync(otherUserIds, cancellationToken).ConfigureAwait(false);
        var rolesMap = await GetUserRolesMapAsync(otherUserIds, cancellationToken).ConfigureAwait(false);

        return participantRows.Select(row =>
        {
            var latest = row.LatestMessage;
            var hasUnread = latest is not null
                && (row.LastReadAtUtc is null || latest.CreatedAtUtc > row.LastReadAtUtc);

            string displayName;
            IReadOnlyList<string> tags = [];
            var isOwnerAccount = false;

            if (row.ParticipantCount > 2)
            {
                displayName = $"{row.ParticipantCount} participants";
            }
            else
            {
                var other = row.OtherParticipants.FirstOrDefault();
                var otherRoles = other?.UserId is not null
                    ? rolesMap.GetValueOrDefault(other.UserId)
                    : null;
                var resolved = ResolveDisplay(
                    other?.UserId,
                    other?.Email,
                    other?.UserName,
                    other?.Nickname,
                    other?.MessageTagsJson,
                    otherRoles,
                    ownerUserIds);
                displayName = resolved.DisplayName;
                tags = resolved.Tags;
                isOwnerAccount = resolved.IsOwnerAccount;
            }

            return new MessageThreadSummary
            {
                ThreadId = row.ThreadId,
                DisplayName = displayName,
                Tags = tags,
                IsOwnerAccount = isOwnerAccount,
                Preview = TruncatePreview(latest?.Body),
                UpdatedAtUtc = row.ThreadUpdatedAtUtc,
                HasUnread = hasUnread,
                ParticipantCount = row.ParticipantCount,
            };
        }).ToList();
    }

    public async Task<int> GetUnreadThreadCountAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var inbox = await GetInboxAsync(userId, cancellationToken).ConfigureAwait(false);
        return inbox.Count(t => t.HasUnread);
    }

    public async Task<MessageThreadDetail?> GetThreadAsync(
        int threadId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var isParticipant = await db.MessageThreadParticipants
            .AnyAsync(p => p.ThreadId == threadId && p.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (!isParticipant)
            return null;

        var thread = await db.MessageThreads
            .AsNoTracking()
            .Where(t => t.Id == threadId)
            .Select(t => new
            {
                t.Id,
                Participants = t.Participants
                    .Select(p => new
                    {
                        p.UserId,
                        p.User.Email,
                        p.User.UserName,
                        p.User.Nickname,
                        p.User.MessageTagsJson,
                    })
                    .ToList(),
                Messages = t.Messages
                    .OrderBy(m => m.CreatedAtUtc)
                    .Select(m => new
                    {
                        m.Id,
                        m.SenderUserId,
                        SenderEmail = m.Sender.Email,
                        SenderUserName = m.Sender.UserName,
                        SenderNickname = m.Sender.Nickname,
                        SenderMessageTagsJson = m.Sender.MessageTagsJson,
                        m.Body,
                        m.CreatedAtUtc,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (thread is null)
            return null;

        var participant = await db.MessageThreadParticipants
            .FirstAsync(p => p.ThreadId == threadId && p.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        participant.LastReadAtUtc = now;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var userIds = thread.Participants.Select(p => p.UserId)
            .Concat(thread.Messages.Select(m => m.SenderUserId))
            .Distinct()
            .ToList();
        var ownerUserIds = await GetOwnerUserIdsAsync(userIds, cancellationToken).ConfigureAwait(false);
        var rolesMap = await GetUserRolesMapAsync(userIds, cancellationToken).ConfigureAwait(false);

        return new MessageThreadDetail
        {
            ThreadId = thread.Id,
            Participants = thread.Participants
                .Select(p =>
                {
                    var resolved = ResolveDisplay(
                        p.UserId,
                        p.Email,
                        p.UserName,
                        p.Nickname,
                        p.MessageTagsJson,
                        rolesMap.GetValueOrDefault(p.UserId),
                        ownerUserIds);
                    return new MessageParticipantInfo
                    {
                        UserId = p.UserId,
                        DisplayName = resolved.DisplayName,
                        Tags = resolved.Tags,
                        IsOwnerAccount = resolved.IsOwnerAccount,
                        Email = p.Email,
                    };
                })
                .ToList(),
            Messages = thread.Messages
                .Select(m =>
                {
                    var resolved = ResolveDisplay(
                        m.SenderUserId,
                        m.SenderEmail,
                        m.SenderUserName,
                        m.SenderNickname,
                        m.SenderMessageTagsJson,
                        rolesMap.GetValueOrDefault(m.SenderUserId),
                        ownerUserIds);
                    return new MessageItem
                    {
                        Id = m.Id,
                        SenderUserId = m.SenderUserId,
                        SenderName = resolved.DisplayName,
                        SenderTags = resolved.Tags,
                        SenderIsOwnerAccount = resolved.IsOwnerAccount,
                        Body = m.Body,
                        CreatedAtUtc = m.CreatedAtUtc,
                        IsMine = m.SenderUserId == userId,
                    };
                })
                .ToList(),
            CanReply = true,
        };
    }

    public async Task<IReadOnlyList<MessageRecipientOption>> GetRecipientOptionsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var sender = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
        if (sender is null)
            return [];

        var senderRoles = await userManager.GetRolesAsync(sender).ConfigureAwait(false);
        if (!MessagingPermissions.CanUseMessaging(senderRoles))
            return [];

        var users = await userManager.Users
            .OrderBy(u => u.Email)
            .ThenBy(u => u.UserName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var ownerUserIds = await GetOwnerUserIdsAsync(users.Select(u => u.Id).ToList(), cancellationToken)
            .ConfigureAwait(false);

        var options = new List<MessageRecipientOption>();
        foreach (var user in users)
        {
            if (user.Id == userId)
                continue;

            var recipientRoles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
            if (!MessagingPermissions.CanStartConversation(senderRoles, recipientRoles))
                continue;

            var resolved = ResolveDisplay(
                user.Id,
                user.Email,
                user.UserName,
                user.Nickname,
                user.MessageTagsJson,
                recipientRoles,
                ownerUserIds);
            options.Add(new MessageRecipientOption
            {
                UserId = user.Id,
                DisplayName = resolved.DisplayName,
                Tags = resolved.Tags,
                Email = user.Email,
                RoleLabel = FormatRoleLabel(recipientRoles, user.MessageTagsJson),
            });
        }

        return options;
    }

    public async Task<SendMessageResult> SendToUserAsync(
        string senderUserId,
        string recipientUserId,
        string body,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateSendAsync(senderUserId, recipientUserId, body, cancellationToken)
            .ConfigureAwait(false);
        if (validation.Error is not null)
            return new SendMessageResult(false, null, validation.Error);

        var threadId = await FindOrCreateDirectThreadAsync(senderUserId, recipientUserId, cancellationToken)
            .ConfigureAwait(false);

        await AppendMessageAsync(threadId, senderUserId, validation.Body!, cancellationToken)
            .ConfigureAwait(false);

        return new SendMessageResult(true, threadId, null);
    }

    public async Task<SendMessageResult> BroadcastToRoleAsync(
        string senderUserId,
        string roleName,
        string body,
        CancellationToken cancellationToken = default)
    {
        var sender = await userManager.FindByIdAsync(senderUserId).ConfigureAwait(false);
        if (sender is null)
            return new SendMessageResult(false, null, "Sender not found.");

        var senderRoles = await userManager.GetRolesAsync(sender).ConfigureAwait(false);
        if (!MessagingPermissions.CanBroadcast(senderRoles))
            return new SendMessageResult(false, null, "Only the owner can send messages to everyone.");

        var normalizedBody = NormalizeBody(body);
        if (normalizedBody is null)
            return new SendMessageResult(false, null, "Message cannot be empty.");

        var recipients = await userManager.GetUsersInRoleAsync(roleName).ConfigureAwait(false);
        var targetUsers = recipients.Where(u => u.Id != senderUserId).ToList();
        if (targetUsers.Count == 0)
            return new SendMessageResult(false, null, $"No users found in role {roleName}.");

        var created = 0;
        int? lastThreadId = null;
        foreach (var recipient in targetUsers)
        {
            var recipientRoles = await userManager.GetRolesAsync(recipient).ConfigureAwait(false);
            if (!MessagingPermissions.CanStartConversation(senderRoles, recipientRoles))
                continue;

            var threadId = await FindOrCreateDirectThreadAsync(senderUserId, recipient.Id, cancellationToken)
                .ConfigureAwait(false);
            await AppendMessageAsync(threadId, senderUserId, normalizedBody, cancellationToken)
                .ConfigureAwait(false);
            created++;
            lastThreadId = threadId;
        }

        if (created == 0)
            return new SendMessageResult(false, null, "No eligible recipients for that broadcast.");

        return new SendMessageResult(true, lastThreadId, null, created);
    }

    public async Task<SendMessageResult> ReplyAsync(
        string senderUserId,
        int threadId,
        string body,
        CancellationToken cancellationToken = default)
    {
        var normalizedBody = NormalizeBody(body);
        if (normalizedBody is null)
            return new SendMessageResult(false, null, "Message cannot be empty.");

        var isParticipant = await db.MessageThreadParticipants
            .AnyAsync(p => p.ThreadId == threadId && p.UserId == senderUserId, cancellationToken)
            .ConfigureAwait(false);

        if (!isParticipant)
            return new SendMessageResult(false, null, "You are not part of this conversation.");

        await AppendMessageAsync(threadId, senderUserId, normalizedBody, cancellationToken)
            .ConfigureAwait(false);

        return new SendMessageResult(true, threadId, null);
    }

    private async Task<HashSet<string>> GetOwnerUserIdsAsync(
        IReadOnlyList<string> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return [];

        var ownerRoleUserIds = await (
            from ur in db.UserRoles
            join r in db.Roles on ur.RoleId equals r.Id
            where r.Name == AppRoles.Owner && userIds.Contains(ur.UserId)
            select ur.UserId
        ).ToListAsync(cancellationToken).ConfigureAwait(false);

        return ownerRoleUserIds.ToHashSet(StringComparer.Ordinal);
    }

    private async Task<Dictionary<string, IReadOnlyList<string>>> GetUserRolesMapAsync(
        IReadOnlyList<string> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
            return [];

        var rows = await (
            from ur in db.UserRoles
            join r in db.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId)
            select new { ur.UserId, RoleName = r.Name! }
        ).ToListAsync(cancellationToken).ConfigureAwait(false);

        return rows
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g
                    .Select(x => x.RoleName)
                    .OrderBy(RoleSortOrder)
                    .ToList());
    }

    private static int RoleSortOrder(string role) => role switch
    {
        AppRoles.Owner => 0,
        AppRoles.CoHost => 1,
        AppRoles.Manager => 2,
        AppRoles.Client => 3,
        _ => 4,
    };

    private sealed record ResolvedUserDisplay(
        string DisplayName,
        IReadOnlyList<string> Tags,
        bool IsOwnerAccount);

    private ResolvedUserDisplay ResolveDisplay(
        string? userId,
        string? email,
        string? userName,
        string? nickname,
        string? messageTagsJson,
        IEnumerable<string>? roles,
        HashSet<string> ownerUserIds)
    {
        var effectiveRoles = NormalizeRoles(roles);
        var isOwner = IsOwnerAccount(userId, email, ownerUserIds)
            || effectiveRoles.Contains(AppRoles.Owner);
        return new ResolvedUserDisplay(
            UserDisplayNames.Format(email, userName, nickname),
            UserMessageTags.ResolveForDisplay(messageTagsJson, effectiveRoles),
            isOwner);
    }

    private static IReadOnlyList<string> NormalizeRoles(IEnumerable<string>? roles)
    {
        var list = roles?
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .ToList();
        return list is null || list.Count == 0 ? [AppRoles.Client] : list;
    }

    private bool IsOwnerAccount(string? userId, string? email, HashSet<string> ownerUserIds) =>
        (!string.IsNullOrEmpty(userId) && ownerUserIds.Contains(userId))
        || UserDisplayNames.IsConfiguredOwnerEmail(email, OwnerEmails);

    private async Task<(string? Body, string? Error)> ValidateSendAsync(
        string senderUserId,
        string recipientUserId,
        string body,
        CancellationToken cancellationToken)
    {
        if (senderUserId == recipientUserId)
            return (null, "You cannot message yourself.");

        var normalizedBody = NormalizeBody(body);
        if (normalizedBody is null)
            return (null, "Message cannot be empty.");

        var sender = await userManager.FindByIdAsync(senderUserId).ConfigureAwait(false);
        var recipient = await userManager.FindByIdAsync(recipientUserId).ConfigureAwait(false);
        if (sender is null || recipient is null)
            return (null, "User not found.");

        var senderRoles = await userManager.GetRolesAsync(sender).ConfigureAwait(false);
        var recipientRoles = await userManager.GetRolesAsync(recipient).ConfigureAwait(false);

        if (!MessagingPermissions.CanStartConversation(senderRoles, recipientRoles))
            return (null, "You are not allowed to message that user.");

        return (normalizedBody, null);
    }

    private async Task<int> FindOrCreateDirectThreadAsync(
        string userAId,
        string userBId,
        CancellationToken cancellationToken)
    {
        var existingThreadId = await (
            from t in db.MessageThreads
            where db.MessageThreadParticipants.Count(p => p.ThreadId == t.Id) == 2
                  && db.MessageThreadParticipants.Any(p => p.ThreadId == t.Id && p.UserId == userAId)
                  && db.MessageThreadParticipants.Any(p => p.ThreadId == t.Id && p.UserId == userBId)
            select t.Id
        ).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (existingThreadId != 0)
            return existingThreadId;

        var now = DateTime.UtcNow;
        var thread = new MessageThread
        {
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        db.MessageThreads.Add(thread);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        db.MessageThreadParticipants.AddRange(
            new MessageThreadParticipant { ThreadId = thread.Id, UserId = userAId },
            new MessageThreadParticipant { ThreadId = thread.Id, UserId = userBId });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return thread.Id;
    }

    private async Task AppendMessageAsync(
        int threadId,
        string senderUserId,
        string body,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        db.Messages.Add(new Message
        {
            ThreadId = threadId,
            SenderUserId = senderUserId,
            Body = body,
            CreatedAtUtc = now,
        });

        var thread = await db.MessageThreads
            .FirstAsync(t => t.Id == threadId, cancellationToken)
            .ConfigureAwait(false);
        thread.UpdatedAtUtc = now;

        var senderParticipant = await db.MessageThreadParticipants
            .FirstOrDefaultAsync(p => p.ThreadId == threadId && p.UserId == senderUserId, cancellationToken)
            .ConfigureAwait(false);
        if (senderParticipant is not null)
            senderParticipant.LastReadAtUtc = now;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string? NormalizeBody(string body)
    {
        var trimmed = body.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        return trimmed.Length <= MaxBodyLength ? trimmed : trimmed[..MaxBodyLength];
    }

    private static string FormatRoleLabel(IEnumerable<string> roles, string? messageTagsJson)
    {
        var tags = UserMessageTags.ResolveForDisplay(messageTagsJson, roles);
        return tags.FirstOrDefault() ?? UserMessageTags.DisplayTagForRole(AppRoles.Client);
    }

    private static string TruncatePreview(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "(No messages yet)";
        var singleLine = body.ReplaceLineEndings(" ").Trim();
        return singleLine.Length <= 120 ? singleLine : singleLine[..117] + "…";
    }
}
