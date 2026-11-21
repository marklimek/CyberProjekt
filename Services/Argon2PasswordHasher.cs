using System.Security.Cryptography;
using System.Text;
using CyberProjekt.Models;
using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Identity;

namespace CyberProjekt.Services
{
    public class Argon2PasswordHasher : IPasswordHasher<User>
    {
        private const int SaltSize = 16;          // 128 bitów losowej soli
        private const int HashSize = 32;          // 256 bitów wynikowego hasha
        private const int MemorySize = 65536;     // 64 MB RAM
        private const int Iterations = 3;         // liczba iteracji
        private const int DegreeOfParallelism = 1;

        public string HashPassword(User user, string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = ComputeHash(password, salt, MemorySize, Iterations, DegreeOfParallelism);

            string saltBase64 = Convert.ToBase64String(salt);
            string hashBase64 = Convert.ToBase64String(hash);

            return $"$argon2id$v=19$m={MemorySize},t={Iterations},p={DegreeOfParallelism}${saltBase64}${hashBase64}";
        }

        public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(providedPassword))
                return PasswordVerificationResult.Failed;

            var parts = hashedPassword.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 5 || parts[0] != "argon2id")
                return PasswordVerificationResult.Failed;

            try
            {
                var paramParts = parts[2].Split(',');
                int memory = int.Parse(paramParts[0].Split('=')[1]);
                int iterations = int.Parse(paramParts[1].Split('=')[1]);
                int parallelism = int.Parse(paramParts[2].Split('=')[1]);

                byte[] salt = Convert.FromBase64String(parts[3]);
                byte[] expectedHash = Convert.FromBase64String(parts[4]);

                byte[] actualHash = ComputeHash(providedPassword, salt, memory, iterations, parallelism);

                // Porównanie stałoczasowe (ochrona przed atakami czasowymi)
                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash)
                    ? PasswordVerificationResult.Success
                    : PasswordVerificationResult.Failed;
            }
            catch
            {
                return PasswordVerificationResult.Failed;
            }
        }

        private static byte[] ComputeHash(string password, byte[] salt, int memorySize, int iterations, int parallelism)
        {
            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                MemorySize = memorySize,
                Iterations = iterations,
                DegreeOfParallelism = parallelism
            };

            return argon2.GetBytes(HashSize);
        }
    }
}