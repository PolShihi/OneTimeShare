using OneTimeShare.Web.Services;
using Xunit;

namespace OneTimeShare.Tests.Services;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        _tokenService = new TokenService();
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidToken()
    {
        
        var (tokenPlain, tokenHash, salt) = _tokenService.GenerateToken();

        
        Assert.NotNull(tokenPlain);
        Assert.NotNull(tokenHash);
        Assert.NotNull(salt);
        Assert.NotEmpty(tokenPlain);
        Assert.NotEmpty(tokenHash);
        Assert.NotEmpty(salt);
        
        
        Assert.DoesNotContain('+', tokenPlain);
        Assert.DoesNotContain('/', tokenPlain);
        Assert.DoesNotContain('=', tokenPlain);
    }

    [Fact]
    public void GenerateToken_ShouldReturnUniqueTokens()
    {
        
        var token1 = _tokenService.GenerateToken();
        var token2 = _tokenService.GenerateToken();

        
        Assert.NotEqual(token1.tokenPlain, token2.tokenPlain);
        Assert.NotEqual(token1.tokenHash, token2.tokenHash);
        Assert.NotEqual(token1.salt, token2.salt);
    }

    [Fact]
    public void VerifyToken_WithValidToken_ShouldReturnTrue()
    {
        
        var (tokenPlain, tokenHash, salt) = _tokenService.GenerateToken();

        
        var result = _tokenService.VerifyToken(tokenPlain, tokenHash, salt);

        
        Assert.True(result);
    }

    [Fact]
    public void VerifyToken_WithInvalidToken_ShouldReturnFalse()
    {
        
        var (_, tokenHash, salt) = _tokenService.GenerateToken();
        var invalidToken = "invalid_token";

        
        var result = _tokenService.VerifyToken(invalidToken, tokenHash, salt);

        
        Assert.False(result);
    }

    [Fact]
    public void VerifyToken_WithInvalidHash_ShouldReturnFalse()
    {
        
        var (tokenPlain, _, salt) = _tokenService.GenerateToken();
        var invalidHash = "invalid_hash";

        
        var result = _tokenService.VerifyToken(tokenPlain, invalidHash, salt);

        
        Assert.False(result);
    }

    [Fact]
    public void VerifyToken_WithInvalidSalt_ShouldReturnFalse()
    {
        
        var (tokenPlain, tokenHash, _) = _tokenService.GenerateToken();
        var invalidSalt = "invalid_salt";

        
        var result = _tokenService.VerifyToken(tokenPlain, tokenHash, invalidSalt);

        
        Assert.False(result);
    }

    [Fact]
    public void VerifyToken_WithNullOrEmptyInputs_ShouldReturnFalse()
    {
        
        var (tokenPlain, tokenHash, salt) = _tokenService.GenerateToken();

        
        Assert.False(_tokenService.VerifyToken(null!, tokenHash, salt));
        Assert.False(_tokenService.VerifyToken("", tokenHash, salt));
        Assert.False(_tokenService.VerifyToken(tokenPlain, null!, salt));
        Assert.False(_tokenService.VerifyToken(tokenPlain, "", salt));
        Assert.False(_tokenService.VerifyToken(tokenPlain, tokenHash, null!));
        Assert.False(_tokenService.VerifyToken(tokenPlain, tokenHash, ""));
    }
}