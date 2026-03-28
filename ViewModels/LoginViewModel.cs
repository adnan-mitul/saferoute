using System.ComponentModel.DataAnnotations;

namespace SafeRoute.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = string.Empty;  // Initialize to avoid null

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty; // Initialize to avoid null

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}
