using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneTimeShare.Web.Data;
using OneTimeShare.Web.Models;
using System.Security.Claims;

namespace OneTimeShare.Web.Controllers;

public class AuthController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext context, ILogger<AuthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback), new { returnUrl })
        };
        
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        
        if (!authenticateResult.Succeeded)
        {
            _logger.LogWarning("Google authentication failed");
            return RedirectToAction("Index", "Home");
        }

        var claims = authenticateResult.Principal?.Claims.ToList() ?? new List<Claim>();
        var subClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var emailClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var nameClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(subClaim) || string.IsNullOrEmpty(emailClaim))
        {
            _logger.LogWarning("Missing required claims from Google authentication");
            return RedirectToAction("Index", "Home");
        }

        // Find or create user account
        var userAccount = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Id == subClaim);
        var now = DateTime.UtcNow;

        if (userAccount == null)
        {
            userAccount = new UserAccount
            {
                Id = subClaim,
                Email = emailClaim,
                DisplayName = nameClaim,
                CreatedAtUtc = now,
                LastLoginAtUtc = now
            };
            
            _context.UserAccounts.Add(userAccount);
            _logger.LogInformation("Created new user account for {Email}", emailClaim);
        }
        else
        {
            // Update last login and potentially email/name if changed
            userAccount.LastLoginAtUtc = now;
            userAccount.Email = emailClaim;
            userAccount.DisplayName = nameClaim;
        }

        await _context.SaveChangesAsync();

        // Create application claims
        var appClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userAccount.Id),
            new(ClaimTypes.Email, userAccount.Email),
            new(ClaimTypes.Name, userAccount.DisplayName ?? userAccount.Email)
        };

        var identity = new ClaimsIdentity(appClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        _logger.LogInformation("User {Email} signed in successfully", emailClaim);

        return LocalRedirect(returnUrl ?? "/");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User signed out");
        return RedirectToAction("Index", "Home");
    }
}