using System.ComponentModel.DataAnnotations;

namespace CyberProjekt.Models
{
    public class SecurityPolicy
    {
        [Key]
        public int Id { get; set; }

        //domyślna minimalna długość = 12
        [Range(8, 128)]
        public int MinPasswordLength { get; set; } = 12;

        // reguła włączana przez administratora (entropia >= 60 bitów)
        public bool IsEntropyPolicyEnabled { get; set; } = true;

        public double MinEntropyBits { get; set; } = 60.0;

        // Ważność hasła w dniach
        [Range(1, 3650)]
        public int PasswordMaxAgeDays { get; set; } = 90;

        // Czas bezczynności sesji w minutach
        [Range(1, 1440)]
        public int SessionIdleTimeoutMinutes { get; set; } = 15;
    }
}
