using System.Security.Cryptography;
using System.Text.Json;

namespace ActualGameSearch.Core.Manifest;

public static class DatasetManifestLoader
{
    public static DatasetManifest Load(string path)
    {
        using var fs = File.OpenRead(path);
        var manifest = JsonSerializer.Deserialize<DatasetManifest>(fs) ?? throw new InvalidDataException("Manifest deserialize null");
        return manifest;
    }

    public static string ComputeSha256(string filePath, int maxRetries = 5, int delayMs = 120)
    {
        using var sha = SHA256.Create();
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var hash = sha.ComputeHash(fs);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }
            catch (IOException) when (attempt < maxRetries - 1)
            {
                Thread.Sleep(delayMs);
            }
        }
        // Final attempt w/o swallow to surface original exception
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        {
            var hash = sha.ComputeHash(fs);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}