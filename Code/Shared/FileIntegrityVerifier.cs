using System;
using System.IO;
using System.Threading.Tasks;

namespace Shared;

public static class FileIntegrityVerifier
{
    public static bool Verify(byte[] receivedBytes, string? expectedHash, out string computedHash)
    {
        computedHash = HashHelper.CalculateSha256(receivedBytes);
        if (string.IsNullOrWhiteSpace(expectedHash))
            return true;
        return string.Equals(computedHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<bool> VerifyFileAndDeleteIfCorruptAsync(string filePath, string? expectedHash)
    {
        string computed = await HashHelper.CalculateSha256(filePath);
        if (string.IsNullOrWhiteSpace(expectedHash))
            return true;
        bool ok = string.Equals(computed, expectedHash, StringComparison.OrdinalIgnoreCase);
        if (!ok && File.Exists(filePath))
            File.Delete(filePath);
        return ok;
    }
}