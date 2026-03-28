using System.ComponentModel.DataAnnotations;

namespace SafeRoute.ViewModels
{
    public class VerifyEmailViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = string.Empty; // initialize to avoid null warnings
    }
}
