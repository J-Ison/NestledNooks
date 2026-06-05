namespace NestledNooks.Data;

public class MessageThread
{
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<MessageThreadParticipant> Participants { get; set; } = [];

    public ICollection<Message> Messages { get; set; } = [];
}
