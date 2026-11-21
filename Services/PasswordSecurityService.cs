using System.Security.Cryptography;
using CyberProjekt.Models;
using Microsoft.AspNetCore.Identity;

namespace CyberProjekt.Services
{
    public class PasswordValidationResult
    {
        public bool IsValid { get; set; }
        public double EntropyBits { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class PasswordSecurityService
    {
        private readonly IPasswordHasher<User> _passwordHasher;

        public PasswordSecurityService(IPasswordHasher<User> passwordHasher)
        {
            _passwordHasher = passwordHasher;
        }

        // Generuje bezpieczny 8-cyfrowy kod liczbowy (Wariant 8)
        public string GenerateOneTimeCode()
        {
            int number = RandomNumberGenerator.GetInt32(0, 100_000_000);
            return number.ToString("D8");
        }

        public string HashOneTimeCode(User user, string code)
        {
            return _passwordHasher.HashPassword(user, code);
        }

        public bool VerifyOneTimeCode(User user, string code)
        {
            if (string.IsNullOrEmpty(user.OneTimeCodeHash) ||
                user.OneTimeCodeUsed ||
                !user.OneTimeCodeExpiresAt.HasValue ||
                user.OneTimeCodeExpiresAt.Value < DateTime.UtcNow)
            {
                return false;
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.OneTimeCodeHash, code);
            return result == PasswordVerificationResult.Success ||
                   result == PasswordVerificationResult.SuccessRehashNeeded;
        }

        // Szacowanie entropii hasła: E = L * log2(N)
        public double CalculateEntropy(string password)
        {
            if (string.IsNullOrEmpty(password)) return 0;

            int poolSize = 0;
            if (password.Any(char.IsLower)) poolSize += 26;
            if (password.Any(char.IsUpper)) poolSize += 26;
            if (password.Any(char.IsDigit)) poolSize += 10;
            if (password.Any(c => !char.IsLetterOrDigit(c))) poolSize += 32;

            if (poolSize == 0) return 0;
            return Math.Round(password.Length * Math.Log2(poolSize), 2);
        }

        public PasswordValidationResult ValidatePassword(string password, SecurityPolicy policy)
        {
            var result = new PasswordValidationResult
            {
                EntropyBits = CalculateEntropy(password)
            };

            if (string.IsNullOrEmpty(password) || password.Length < policy.MinPasswordLength)
            {
                result.Errors.Add($"Hasło musi mieć co najmniej {policy.MinPasswordLength} znaków.");
            }

            if (policy.IsEntropyPolicyEnabled && result.EntropyBits < policy.MinEntropyBits)
            {
                var hints = new List<string>();
                if (!password.Any(char.IsUpper)) hints.Add("wielkie litery");
                if (!password.Any(char.IsDigit)) hints.Add("cyfry");
                if (!password.Any(c => !char.IsLetterOrDigit(c))) hints.Add("znaki specjalne");

                string hintText = hints.Any()
                    ? $"Wydłuż hasło lub dodaj: {string.Join(", ", hints)}."
                    : "Wydłuż hasło o dodatkowe znaki.";

                result.Errors.Add(
                    $"Szacowana entropia hasła wynosi {result.EntropyBits} bitów (wymagane min. {policy.MinEntropyBits} bitów). Podpowiedź: {hintText}");
            }

            result.IsValid = !result.Errors.Any();
            return result;
        }

        public bool IsPasswordInHistory(User user, string newPassword, IEnumerable<PasswordHistory> histories)
        {
            foreach (var entry in histories)
            {
                if (string.IsNullOrEmpty(entry.PasswordHash)) continue;

                var verify = _passwordHasher.VerifyHashedPassword(user, entry.PasswordHash, newPassword);
                if (verify == PasswordVerificationResult.Success ||
                    verify == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    return true;
                }
            }

            return false;
        }
    }
}