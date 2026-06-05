using System.ComponentModel.DataAnnotations;
using NestledNooks.Models;

namespace NestledNooks.Tests.Models;

public sealed class ContactFormModelTests
{
    [Fact]
    public void Validate_SignedInUser_IgnoresEmptyEmailField()
    {
        var model = new ContactFormModel
        {
            RequireIdentityFields = false,
            Email = "",
            Message = "Question about my booking.",
        };

        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

        Assert.True(valid, string.Join("; ", results.Select(r => r.ErrorMessage)));
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_AnonymousUser_RequiresNameAndEmail()
    {
        var model = new ContactFormModel
        {
            RequireIdentityFields = true,
            Name = "",
            Email = "",
            Message = "Hello there",
        };

        var results = model.Validate(new ValidationContext(model)).ToList();

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ContactFormModel.Name)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(ContactFormModel.Email)));
    }
}
