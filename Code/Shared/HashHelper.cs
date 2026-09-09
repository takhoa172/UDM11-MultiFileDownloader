using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    public static class HashHelper
    {
        public static async Task<string> CalculateSha256(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Error: Duong dan file khong duoc trong", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Error: Khong the tim thay file", filePath);
            }

            await using var fileStream = new FileStream
            (
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true
            );
            using var sha256 = SHA256.Create();

            byte[] hashBytes = await sha256.ComputeHashAsync(fileStream);

            return Convert.ToHexString(hashBytes).ToLower();
        }

        public static string CalculateSha256(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(data);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        //public static string CalculateSha256(string content)
        //{
        //    return CalculateSha256(Encoding.UTF8.GetBytes(content ?? string.Empty));
        //}
    }
}