using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Application.Services;

public class SignedConsentEvidenceCertificate
{
    public Guid DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public string DocumentSnapshot { get; set; } = string.Empty;
    public string ChildName { get; set; } = string.Empty;
    public string SignerName { get; set; } = string.Empty;
    public string SignerEmail { get; set; } = string.Empty;
    public string SignerRelationship { get; set; } = string.Empty;
    public string LegalIntentConfirmation { get; set; } = string.Empty;
    public DateTime SignedAtUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, string> Choices { get; set; } = new();
    public string Sha256Checksum { get; set; } = string.Empty;
}

public interface IConsentStorageService
{
    Task<(string storageKey, string checksum)> StoreSignedCertificateAsync(SignedConsentEvidenceCertificate certificate, CancellationToken ct = default);
    Task<SignedConsentEvidenceCertificate?> RetrieveSignedCertificateAsync(string storageKey, CancellationToken ct = default);
}

public class LocalConsentStorageService : IConsentStorageService
{
    private readonly string _storageDirectory;

    public LocalConsentStorageService()
    {
        // Store in AppData or local directory
        var baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "SignedConsents");
        Directory.CreateDirectory(baseDir);
        _storageDirectory = baseDir;
    }

    public async Task<(string storageKey, string checksum)> StoreSignedCertificateAsync(SignedConsentEvidenceCertificate certificate, CancellationToken ct = default)
    {
        var rawJson = JsonSerializer.Serialize(certificate, new JsonSerializerOptions { WriteIndented = true });
        
        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawJson));
        var checksum = Convert.ToHexString(hashBytes).ToLowerInvariant();
        certificate.Sha256Checksum = checksum;

        // Re-serialize with checksum included
        var finalJson = JsonSerializer.Serialize(certificate, new JsonSerializerOptions { WriteIndented = true });
        var fileName = $"{certificate.DocumentId}_{DateTime.UtcNow.Ticks}.json";
        var fullPath = Path.Combine(_storageDirectory, fileName);

        await File.WriteAllTextAsync(fullPath, finalJson, ct);
        return (fileName, checksum);
    }

    public async Task<SignedConsentEvidenceCertificate?> RetrieveSignedCertificateAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_storageDirectory, storageKey);
        if (!File.Exists(fullPath)) return null;

        var json = await File.ReadAllTextAsync(fullPath, ct);
        return JsonSerializer.Deserialize<SignedConsentEvidenceCertificate>(json);
    }
}
