using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneTimeShare.Web.Data;

namespace OneTimeShare.Web.Controllers;

[AllowAnonymous]
[ApiController]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(AppDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("/health")]
    [HttpGet("/health/live")]
    public IActionResult Live()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    [HttpGet("/health/ready")]
    public async Task<IActionResult> Ready()
    {
        try
        {
            
            await _context.Database.CanConnectAsync();
            
            return Ok(new 
            { 
                status = "ready", 
                timestamp = DateTime.UtcNow,
                checks = new
                {
                    database = "healthy"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(503, new 
            { 
                status = "unhealthy", 
                timestamp = DateTime.UtcNow,
                error = "Database connection failed"
            });
        }
    }
}