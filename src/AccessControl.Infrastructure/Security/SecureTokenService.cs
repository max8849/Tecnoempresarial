using System.Security.Cryptography;
using System.Text;
using AccessControl.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AccessControl.Infrastructure.Security;

public class SecureTokenService : ISecureTokenService
{
    private readonly string _pepper;

    public SecureTokenService(IConfiguration config)
    {
        _pepper = config["Security:TokenPepper"] ?? throw new InvalidOperationException("Falta Security:TokenPepper en la configuración.");
    }

    public (string SelectorId, string Verifier, string VerifierHash) GenerateNewToken()
    {
        var selectorId = GenerateUrlSafeRandom(12);
        var verifier = GenerateUrlSafeRandom(32);
        var hash = HashVerifier(verifier);
        return (selectorId, verifier, hash);
    }

    public string HashVerifier(string verifier)
    {
        var bytes = Encoding.UTF8.GetBytes(verifier + _pepper);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    public bool TryParseToken(string rawToken, out string selectorId, out string verifier)
    {
        selectorId = string.Empty;
        verifier = string.Empty;

        if (string.IsNullOrWhiteSpace(rawToken)) return false;

        var separatorIndex = rawToken.IndexOf('.');
        if (separatorIndex <= 0 || separatorIndex == rawToken.Length - 1) return false;

        selectorId = rawToken[..separatorIndex];
        verifier = rawToken[(separatorIndex + 1)..];
        return true;
    }

    public bool VerifyHash(string verifier, string storedHash)
    {
        var computed = Encoding.UTF8.GetBytes(HashVerifier(verifier));
        var stored = Encoding.UTF8.GetBytes(storedHash);
        return computed.Length == stored.Length && CryptographicOperations.FixedTimeEquals(computed, stored);
    }

    private static string GenerateUrlSafeRandom(int byteLength)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
