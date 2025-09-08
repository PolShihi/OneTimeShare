using System.Security.Cryptography;
using System.Text;

namespace OneTimeShare.Web.Services;

public class TokenService : ITokenService
{
    private const int TokenLengthBytes = 32; // 256 bits
    private const int SaltLengthBytes = 16; // 128 bits

    public (string tokenPlain, string tokenHash, string salt) GenerateToken()
    {
        // Generate cryptographically secure random token
        var tokenBytes = new byte[TokenLengthBytes];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }
        var tokenPlain = Convert.ToBase64String(tokenBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        // Generate salt
        var saltBytes = new byte[SaltLengthBytes];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        var salt = Convert.ToBase64String(saltBytes);

        // Hash token with salt
        var tokenHash = HashToken(tokenPlain, salt);

        return (tokenPlain, tokenHash, salt);
    }

    public bool VerifyToken(string tokenPlain, string tokenHash, string salt)
    {
        if (string.IsNullOrEmpty(tokenPlain) || string.IsNullOrEmpty(tokenHash) || string.IsNullOrEmpty(salt))
        {
            return false;
        }

        try
        {
            var computedHash = HashToken(tokenPlain, salt);
            
            // Constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHash),
                Encoding.UTF8.GetBytes(tokenHash)
            );
        }
        catch
        {
            return false;
        }
    }

    private static string HashToken(string token, string salt)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var saltBytes = Convert.FromBase64String(salt);
        
        using var sha256 = SHA256.Create();
        var combined = new byte[tokenBytes.Length + saltBytes.Length];
        Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
        Buffer.BlockCopy(tokenBytes, 0, combined, saltBytes.Length, tokenBytes.Length);
        
        var hashBytes = sha256.ComputeHash(combined);
        return Convert.ToBase64String(hashBytes);
    }
}