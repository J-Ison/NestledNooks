namespace NestledNooks.Services;

public interface IMessagingService
{
    Task<IReadOnlyList<MessageThreadSummary>> GetInboxAsync(string userId, CancellationToken cancellationToken = default);

    Task<int> GetUnreadThreadCountAsync(string userId, CancellationToken cancellationToken = default);

    Task<MessageThreadDetail?> GetThreadAsync(int threadId, string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageRecipientOption>> GetRecipientOptionsAsync(string userId, CancellationToken cancellationToken = default);

    Task<SendMessageResult> SendToUserAsync(string senderUserId, string recipientUserId, string body, CancellationToken cancellationToken = default);

    Task<SendMessageResult> BroadcastToRoleAsync(string senderUserId, string roleName, string body, CancellationToken cancellationToken = default);

    Task<SendMessageResult> ReplyAsync(string senderUserId, int threadId, string body, CancellationToken cancellationToken = default);
}

public sealed class MessageThreadSummary
{
    public int ThreadId { get; init; }
    public string DisplayName { get; init; } = "";
    public IReadOnlyList<string> Tags { get; init; } = [];
    public bool IsOwnerAccount { get; init; }
    public string Preview { get; init; } = "";
    public DateTime UpdatedAtUtc { get; init; }
    public bool HasUnread { get; init; }
    public int ParticipantCount { get; init; }
}

public sealed class MessageThreadDetail
{
    public int ThreadId { get; init; }
    public IReadOnlyList<MessageParticipantInfo> Participants { get; init; } = [];
    public IReadOnlyList<MessageItem> Messages { get; init; } = [];
    public bool CanReply { get; init; }
}

public sealed class MessageParticipantInfo
{
    public string UserId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public IReadOnlyList<string> Tags { get; init; } = [];
    public bool IsOwnerAccount { get; init; }
    public string? Email { get; init; }
}

public sealed class MessageItem
{
    public int Id { get; init; }
    public string SenderUserId { get; init; } = "";
    public string SenderName { get; init; } = "";
    public IReadOnlyList<string> SenderTags { get; init; } = [];
    public bool SenderIsOwnerAccount { get; init; }
    public string Body { get; init; } = "";
    public DateTime CreatedAtUtc { get; init; }
    public bool IsMine { get; init; }
}

public sealed class MessageRecipientOption
{
    public string UserId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string? Email { get; init; }
    public string RoleLabel { get; init; } = "";
}

public sealed record SendMessageResult(bool Succeeded, int? ThreadId, string? ErrorMessage, int ThreadsCreated = 1);
