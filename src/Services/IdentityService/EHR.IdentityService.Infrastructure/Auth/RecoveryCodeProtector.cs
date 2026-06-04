using System.Security.Cryptography;
using System.Text;
using EHR.IdentityService.Application.Auth;

namespace EHR.IdentityService.Infrastructure.Auth;

public sealed class RecoveryCodeProtector : IRecoveryCodeProtector
{
    public IReadOnlyCollection<string> GenerateCodes(int count = 10) =>
        Enumerable.Range(0, count)
            .Select(_ => Convert.ToHexString(RandomNumberGenerator.GetBytes(5)))
            .ToArray();

    public string HashCodes(IEnumerable<string> codes) =>
        string.Join('|', codes.Select(HashCode));

    public bool TryConsume(string code, string recoveryCodesHash, out string updatedRecoveryCodesHash)
    {
        var hashes = recoveryCodesHash.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
        var incoming = HashCode(code);
        var index = hashes.FindIndex(hash => FixedEquals(hash, incoming));
        if (index < 0)
        {
            updatedRecoveryCodesHash = recoveryCodesHash;
            return false;
        }

        hashes.RemoveAt(index);
        updatedRecoveryCodesHash = string.Join('|', hashes);
        return true;
    }

    private static string HashCode(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim().ToUpperInvariant()));
        return Convert.ToBase64String(bytes);
    }

    private static bool FixedEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(left), Convert.FromBase64String(right));
}
