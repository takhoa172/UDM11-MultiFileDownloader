using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Shared
{
    public static class HashHelper
    {
        public static string CalculateSha256(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception("Error: Khong tim thay duong dan");
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    byte[] hashByte = sha256.ComputeHash(fileStream);

                    StringBuilder result = new StringBuilder();
                    for (int i = 0; i < hashByte.Length; i++)
                    {
                        result.Append(hashByte[i].ToString("x2"));
                    }

                    return result.ToString();
                }
            }
        }
    }
}
