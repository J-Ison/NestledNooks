namespace NestledNooks.Data;

public sealed class AdminBookingSeen
{
    public string UserId { get; set; } = "";
    public ApplicationUser? User { get; set; }

    public int BookingRequestId { get; set; }
    public BookingRequest? BookingRequest { get; set; }

    public DateTime SeenAtUtc { get; set; }
}
