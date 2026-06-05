using System.ComponentModel.DataAnnotations;

namespace NestledNooks.Models;

public sealed class ContactFormModel : IValidatableObject
{
    /// <summary>When false, name/email are taken from the signed-in account instead of the form.</summary>
    public bool RequireIdentityFields { get; set; } = true;

    [Display(Name = "Your name")]
    public string Name { get; set; } = "";

    /// <summary>Validated in <see cref="Validate"/> when <see cref="RequireIdentityFields"/> is true.</summary>
    [Display(Name = "Your email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter a message.")]
    [MinLength(5, ErrorMessage = "Please enter a message (at least 5 characters).")]
    [Display(Name = "Message")]
    public string Message { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!RequireIdentityFields)
            yield break;

        if (string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("Please enter your name.", [nameof(Name)]);

        if (string.IsNullOrWhiteSpace(Email))
            yield return new ValidationResult("Please enter your email.", [nameof(Email)]);
        else if (!new EmailAddressAttribute().IsValid(Email))
            yield return new ValidationResult("Please enter a valid email address.", [nameof(Email)]);
    }
}
