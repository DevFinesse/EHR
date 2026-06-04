using System.Security.Cryptography;
using EHR.IdentityService.Application.Auth;

namespace EHR.IdentityService.Infrastructure.Auth;

public sealed class Rfc6238TotpProvider : ITotpProvider
{
    private const int StepSeconds = 30;
    private const int Digits = 6;

    public string CreateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        return Convert.ToBase64String(bytes);
    }

    public bool Verify(string secret, string code, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != Digits)
        {
            return false;
        }

        var secretBytes = Convert.FromBase64String(secret);
        var currentStep = now.ToUnixTimeSeconds() / StepSeconds;
        for (var offset = -1; offset <= 1; offset++)
        {
            if (CreateCode(secretBytes, currentStep + offset) == code)
            {
                return true;
            }
        }

        return false;
    }

    private static string CreateCode(byte[] secret, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes);
        var offset = hash[^1] & 0x0f;
        var binary =
            ((hash[offset] & 0x7f) << 24) |
            ((hash[offset + 1] & 0xff) << 16) |
            ((hash[offset + 2] & 0xff) << 8) |
            (hash[offset + 3] & 0xff);

        var otp = binary % (int)Math.Pow(10, Digits);
        return otp.ToString($"D{Digits}");
    }
}
