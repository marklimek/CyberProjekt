using System.ComponentModel.DataAnnotations;

namespace CyberProjekt.Models.ViewModels
{
    public class ChangePasswordViewModel
    {
        [DataType(DataType.Password)]
        [Display(Name = "Aktualne hasło")]
        public string? CurrentPassword { get; set; }

        [Required(ErrorMessage = "Podaj nowe hasło.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nowe hasło")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Powtórz nowe hasło.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Nowe hasła muszą być identyczne.")]
        [Display(Name = "Powtórz nowe hasło")]
        public string ConfirmNewPassword { get; set; } = string.Empty;

        public bool IsForcedChange { get; set; }
    }
}