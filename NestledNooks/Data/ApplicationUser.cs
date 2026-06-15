using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

public class ApplicationUser : IdentityUser
{
    [MaxLength(50)]
    public string? Nickname { get; set; }

    /// <summary>JSON array of message display tags (max 5), e.g. ["Host","Owner"].</summary>
    [MaxLength(500)]
    public string? MessageTagsJson { get; set; }

    [Required]
    [Phone]
    [MaxLength(20)]
    public override string? PhoneNumber { get; set; }

    public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;
}
