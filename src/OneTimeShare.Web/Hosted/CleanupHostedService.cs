using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OneTimeShare.Web.Data;
using OneTimeShare.Web.Models.Options;
using OneTimeShare.Web.Services;

namespace OneTimeShare.Web.Hosted;

public class CleanupHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CleanupHostedService> _logger;
    private readonly TimeSpan _cleanupInterval;

    public CleanupHostedService(IServiceProvider serviceProvider, ILogger<CleanupHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        
        var intervalMinutes = int.Parse(Environment.GetEnvironmentVariable("CLEANUP_INTERVAL_MINUTES") ?? "1440");
        _cleanupInterval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cleanup service starting. Interval: {Interval}", _cleanupInterval);

        
        await PerformCleanupAsync(stoppingToken);

        
        using var timer = new PeriodicTimer(_cleanupInterval);
        
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PerformCleanupAsync(stoppingToken);
        }
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
            
            var now = DateTime.UtcNow;
            var deletedCount = 0;
            var orphanedFilesDeleted = 0;

            _logger.LogInformation("Starting cleanup process at {Now}", now);

            
            var expiredFiles = await context.StoredFiles
                .Where(f => f.ExpiresAtUtc.HasValue && f.ExpiresAtUtc.Value < now && f.DeletedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var file in expiredFiles)
            {
                try
                {
                    
                    file.DeletedAtUtc = now;
                    file.ConcurrencyStamp = Guid.NewGuid().ToString();
                    
                    await context.SaveChangesAsync(cancellationToken);
                    
                    
                    if (storageService.FileExists(file.StoragePath))
                    {
                        await storageService.DeleteAsync(file.StoragePath);
                    }
                    
                    deletedCount++;
                    _logger.LogInformation("Cleaned up expired file {FileId}", file.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to clean up expired file {FileId}", file.Id);
                }
            }

            
            var deletedFiles = await context.StoredFiles
                .Where(f => f.DeletedAtUtc.HasValue)
                .Select(f => new { f.Id, f.StoragePath })
                .ToListAsync(cancellationToken);

            foreach (var file in deletedFiles)
            {
                try
                {
                    if (storageService.FileExists(file.StoragePath))
                    {
                        await storageService.DeleteAsync(file.StoragePath);
                        orphanedFilesDeleted++;
                        _logger.LogInformation("Cleaned up orphaned file {FileId}", file.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to clean up orphaned file {FileId}", file.Id);
                }
            }

            
            var cutoffDate = now.AddDays(-(int.Parse(Environment.GetEnvironmentVariable("FILE_RETENTION_DAYS") ?? "30") + 7));
            var oldRecords = await context.StoredFiles
                .Where(f => f.DeletedAtUtc.HasValue && f.DeletedAtUtc.Value < cutoffDate)
                .ToListAsync(cancellationToken);

            if (oldRecords.Any())
            {
                context.StoredFiles.RemoveRange(oldRecords);
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Removed {Count} old database records", oldRecords.Count);
            }

            _logger.LogInformation("Cleanup completed. Expired files deleted: {DeletedCount}, Orphaned files cleaned: {OrphanedCount}", 
                deletedCount, orphanedFilesDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup process");
        }
    }
}