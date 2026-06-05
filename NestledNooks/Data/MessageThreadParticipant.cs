namespace NestledNooks.Data;

public class MessageThreadParticipant
{
    public int ThreadId { get; set; }

    public MessageThread Thread { get; set; } = null!;

    public string UserId { get; set; } = "";

    public ApplicationUser User { get; set; } = null!;

    public DateTime? LastReadAtUtc { get; set; }
}
