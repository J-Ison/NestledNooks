using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

public class ApplicationUser : IdentityUser
{
    [Required]
    [Phone]
    [MaxLength(20)]
    public override string? PhoneNumber { get; set; }
}
