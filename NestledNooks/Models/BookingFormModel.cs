using System.ComponentModel.DataAnnotations;

namespace NestledNooks.Models;

public sealed class BookingFormModel : IValidatableObject
{
    public const string DeerfieldSlug = "deerfield-retreat";

    [Required(ErrorMessage = "Choose a property.")]
    [Display(Name = "Property")]
    public string PropertySlug { get; set; } = DeerfieldSlug;

    [Required(ErrorMessage = "Please enter your full name.")]
    [Display(Name = "Full name")]
    [StringLength(200, MinimumLength = 2)]
    public string GuestFullName { get; set; } = "";

    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress]
    [Display(Name = "Email")]
    [StringLength(256)]
    public string GuestEmail { get; set; } = "";

    [Phone]
    [Display(Name = "Phone (optional)")]
    [StringLength(40)]
    public string? GuestPhone { get; set; }

    [Required(ErrorMessage = "Please choose a check-in date.")]
    [Display(Name = "Check-in")]
    public DateOnly? CheckIn { get; set; }

    [Required(ErrorMessage = "Please choose a check-out date.")]
    [Display(Name = "Check-out")]
    public DateOnly? CheckOut { get; set; }

    [Range(1, 12, ErrorMessage = "Guest count must be between 1 and 12.")]
    [Display(Name = "Guests")]
    public int GuestCount { get; set; } = 2;

    [Range(0, 6, ErrorMessage = "Pet count must be between 0 and 6.")]
    [Display(Name = "Pets")]
    public int PetCount { get; set; }

    [Display(Name = "Questions or requests (optional)")]
    [StringLength(2000)]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (CheckIn is { } ci)
        {
            if (ci < today)
                yield return new ValidationResult("Check-in cannot be in the past.", new[] { nameof(CheckIn) });
        }

        if (CheckIn is { } cin && CheckOut is { } cout)
        {
            if (cout <= cin)
                yield return new ValidationResult("Check-out must be after check-in.", new[] { nameof(CheckOut) });
        }
    }
}
