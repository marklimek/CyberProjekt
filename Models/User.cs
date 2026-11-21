using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CyberProjekt.Models
{
    public class User : IdentityUser
    {

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        // Blokowanie konta przez administratora[cite: 1]
        public bool IsBlocked { get; set; } = false;

        // Wymuszenie zmiany hasła przy pierwszym logowaniu lub po resecie[cite: 1]
        public bool MustChangePassword { get; set; } = true;

        // Data ostatniej zmiany hasła (do sprawdzania ważności hasła w dniach)[cite: 1]
        public DateTime PasswordChangedAt { get; set; } = DateTime.UtcNow;

        // Hasło jednorazowe (Wariant 8: kod liczbowy 8 cyfr, ważny 1 h)[cite: 1]
        public string? OneTimeCodeHash { get; set; }
        public DateTime? OneTimeCodeExpiresAt { get; set; }
        public bool OneTimeCodeUsed { get; set; } = false;

        // Zadanie dodatkowe (na ocenę bdb): TOTP 2FA[cite: 1]
        public string? TotpSecret { get; set; }
        public bool IsTotpEnabled { get; set; } = false;

        // Relacje z istniejącymi modelami[cite: 3, 6, 8]
        public ICollection<ProjectTask> AssignedTasks { get; set; } = new List<ProjectTask>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();
    }
}