namespace NestledNooks.Models;

public sealed record BookingQuote(
    int Nights,
    decimal NightlyRate,
    decimal Subtotal,
    decimal CleaningFee,
    decimal PetFee,
    decimal TotalAmount,
    bool UsesDynamicPricing = false,
    decimal DiscountAmount = 0,
    string? DiscountLabel = null)
{
    public bool HasDiscount => DiscountAmount > 0;

    public decimal OriginalTotal => TotalAmount + DiscountAmount;
}
