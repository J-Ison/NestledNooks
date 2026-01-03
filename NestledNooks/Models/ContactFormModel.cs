using System.ComponentModel.DataAnnotations;

namespace NestledNooks.Models
{
    public sealed class ContactFormModel
    {
        [Required]
        [Display(Name = "Your name")]
        public string Name { get; set; } = "";

        [Required]
        [EmailAddress]
        [Display(Name = "Your email")]
        public string Email { get; set; } = "";

        [Required]
        [MinLength(5)]
        [Display(Name = "Message")]
        public string Message { get; set; } = "";
    }
}
