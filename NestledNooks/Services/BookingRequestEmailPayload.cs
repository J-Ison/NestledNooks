namespace NestledNooks.Services;

public sealed record BookingRequestEmailPayload(
    int RequestId,
    string BookingNumber,
    string PropertyDisplayName,
    string GuestFullName,
    string GuestEmail,
    string? GuestPhone,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int GuestCount,
    int PetCount,
    int NightCount,
    decimal TotalAmount,
    string? Notes);

public sealed record BookingStatusEmailPayload(
    string BookingNumber,
    string PropertyDisplayName,
    string GuestFullName,
    string GuestEmail,
    DateOnly CheckIn,
    DateOnly CheckOut,
    decimal TotalAmount,
    string OldStatus,
    string NewStatus,
    string? StatusNote);
