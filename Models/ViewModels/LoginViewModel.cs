using System.ComponentModel.DataAnnotations;

namespace CyberProjekt.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Podaj login.")]
        [Display(Name = "Login")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Podaj hasło lub 8-cyfrowy kod jednorazowy.")]
        [DataType(DataType.Password)] // Maskowanie hasła
        [Display(Name = "Hasło")]
        public string Password { get; set; } = string.Empty;
    }
}