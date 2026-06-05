namespace NestledNooks.Data;

public class Message
{
    public int Id { get; set; }

    public int ThreadId { get; set; }

    public MessageThread Thread { get; set; } = null!;

    public string SenderUserId { get; set; } = "";

    public ApplicationUser Sender { get; set; } = null!;

    public string Body { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }
}
