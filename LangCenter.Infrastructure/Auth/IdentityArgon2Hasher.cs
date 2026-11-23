using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using LangCenter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace LangCenter.Infrastructure.Auth;

public class IdentityArgon2Hasher : IPasswordHasher<AppUser>
{
    private const string Format = "argon2id-v1";
    private const int SaltLength = 16;
    private const int HashLength = 32;

    public string HashPassword(AppUser user, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = Compute(password, salt);
        return $"{Format}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public PasswordVerificationResult VerifyHashedPassword(
        AppUser user, string hashedPassword, string providedPassword)
    {
        var parts = hashedPassword.Split('$');
        if (parts.Length != 3 || parts[0] != Format)
            return PasswordVerificationResult.Failed;

        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);
            if (salt.Length != SaltLength || expected.Length != HashLength)
                return PasswordVerificationResult.Failed;

            var actual = Compute(providedPassword, salt);
            return CryptographicOperations.FixedTimeEquals(actual, expected)
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }
    }

    private static byte[] Compute(string password, byte[] salt)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        try
        {
            using var argon2 = new Argon2id(passwordBytes)
            {
                Salt = salt,
                MemorySize = 65_536,
                Iterations = 4,
                DegreeOfParallelism = 2
            };
            return argon2.GetBytes(HashLength);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }
}
