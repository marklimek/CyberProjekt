using System.ComponentModel.DataAnnotations;

namespace CyberProjekt.Models.ViewModels
{
    public class AdminUserFormViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Login jest wymagany.")]
        [Display(Name = "Login")]
        public string Username { get; set; } = string.Empty;

        [EmailAddress]
        [Display(Name = "Adres e-mail")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Imię jest wymagane.")]
        [Display(Name = "Imię")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nazwisko jest wymagane.")]
        [Display(Name = "Nazwisko")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wybierz rolę.")]
        [Display(Name = "Rola")]
        public int RoleId { get; set; }

        [Display(Name = "Konto zablokowane")]
        public bool IsBlocked { get; set; }
    }
}