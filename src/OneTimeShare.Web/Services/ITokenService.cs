namespace OneTimeShare.Web.Services;

public interface ITokenService
{
    (string tokenPlain, string tokenHash, string salt) GenerateToken();
    bool VerifyToken(string tokenPlain, string tokenHash, string salt);
}