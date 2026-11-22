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

        // Blokowanie konta przez administratora
        public bool IsBlocked { get; set; } = false;

        // Wymuszenie zmiany hasła przy pierwszym logowaniu lub po resecie
        public bool MustChangePassword { get; set; } = true;

        // Data ostatniej zmiany hasła
        public DateTime PasswordChangedAt { get; set; } = DateTime.UtcNow;

        // Hasło jednorazowe
        public string? OneTimeCodeHash { get; set; }
        public DateTime? OneTimeCodeExpiresAt { get; set; }
        public bool OneTimeCodeUsed { get; set; } = false;

        public ICollection<ProjectTask> AssignedTasks { get; set; } = new List<ProjectTask>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();
    }
}