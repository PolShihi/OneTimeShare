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
        // Act
        var (tokenPlain, tokenHash, salt) = _tokenService.GenerateToken();

        // Assert
        Assert.NotNull(tokenPlain);
        Assert.NotNull(tokenHash);
        Assert.NotNull(salt);
        Assert.NotEmpty(tokenPlain);
        Assert.NotEmpty(tokenHash);
        Assert.NotEmpty(salt);
        
        // Token should be URL-safe base64
        Assert.DoesNotContain('+', tokenPlain);
        Assert.DoesNotContain('/', tokenPlain);
        Assert.DoesNotContain('=', tokenPlain);
    }

    [Fact]
    public void GenerateToken_ShouldReturnUniqueTokens()
    {
        // Act
        var token1 = _tokenService.GenerateToken();
        var token2 = _tokenService.GenerateToken();

        // Assert
        Assert.NotEqual(token1.tokenPlain, token2.tokenPlain);
        Assert.NotEqual(token1.tokenHash, token2.tokenHash);
        Assert.NotEqual(token1.salt, token2.salt);
    }

    [Fact]
    public void VerifyToken_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var (tokenPlain, tokenHash, salt) = _tokenService.GenerateToken();

        // Act
        var result = _tokenService.VerifyToken(tokenPlain, tokenHash, salt);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyToken_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var (_, tokenHash, salt) = _tokenService.GenerateToken();
        var invalidToken = "invalid_token";

        // Act
        var result = _tokenService.VerifyToken(invalidToken, tokenHash, salt);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyToken_WithInvalidHash_ShouldReturnFalse()
    {
        // Arrange
        var (tokenPlain, _, salt) = _tokenService.GenerateToken();
        var invalidHash = "invalid_hash";

        // Act
        var result = _tokenService.VerifyToken(tokenPlain, invalidHash, salt);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyToken_WithInvalidSalt_ShouldReturnFalse()
    {
        // Arrange
        var (tokenPlain, tokenHash, _) = _tokenService.GenerateToken();
        var invalidSalt = "invalid_salt";

        // Act
        var result = _tokenService.VerifyToken(tokenPlain, tokenHash, invalidSalt);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyToken_WithNullOrEmptyInputs_ShouldReturnFalse()
    {
        // Arrange
        var (tokenPlain, tokenHash, salt) = _tokenService.GenerateToken();

        // Act & Assert
        Assert.False(_tokenService.VerifyToken(null!, tokenHash, salt));
        Assert.False(_tokenService.VerifyToken("", tokenHash, salt));
        Assert.False(_tokenService.VerifyToken(tokenPlain, null!, salt));
        Assert.False(_tokenService.VerifyToken(tokenPlain, "", salt));
        Assert.False(_tokenService.VerifyToken(tokenPlain, tokenHash, null!));
        Assert.False(_tokenService.VerifyToken(tokenPlain, tokenHash, ""));
    }
}