using System;
using System.Security.Cryptography;

namespace Shared
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        public static string GenerateSalt()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltSize));
        }

        public static string HashPassword(string password, string salt)
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password, saltBytes, Iterations, HashAlgorithmName.SHA256, HashSize);
            return Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string salt, string expectedHash)
        {
            string computed = HashPassword(password, salt);
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(computed),
                Convert.FromBase64String(expectedHash));
        }
    }
}
