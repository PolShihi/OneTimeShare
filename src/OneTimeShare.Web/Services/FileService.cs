using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OneTimeShare.Web.Data;
using OneTimeShare.Web.Models;
using OneTimeShare.Web.Models.Options;

namespace OneTimeShare.Web.Services;

public class FileService : IFileService
{
    private readonly AppDbContext _context;
    private readonly IStorageService _storageService;
    private readonly ITokenService _tokenService;
    private readonly OneTimeShare.Web.Models.Options.FileOptions _fileOptions;
    private readonly ILogger<FileService> _logger;

    public FileService(
        AppDbContext context,
        IStorageService storageService,
        ITokenService tokenService,
        IOptions<OneTimeShare.Web.Models.Options.FileOptions> fileOptions,
        ILogger<FileService> logger)
    {
        _context = context;
        _storageService = storageService;
        _tokenService = tokenService;
        _fileOptions = fileOptions.Value;
        _logger = logger;
    }

    public async Task<(StoredFile storedFile, string oneTimeToken)> CreateUploadRecordAsync(
        string userId, 
        string originalFileName, 
        string contentType, 
        Stream fileStream)
    {
        // Generate one-time token
        var (tokenPlain, tokenHash, salt) = _tokenService.GenerateToken();
        
        // Get file extension safely
        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrEmpty(extension))
        {
            extension = ".bin"; // Default extension for files without one
        }

        // Save file to storage
        var (storagePath, sizeBytes) = await _storageService.SaveAsync(fileStream, extension);

        // Create database record
        var storedFile = new StoredFile
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            OriginalFileName = Path.GetFileName(originalFileName), // Sanitize filename
            ContentType = contentType,
            SizeBytes = sizeBytes,
            StoragePath = storagePath,
            UploadAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_fileOptions.RetentionDays),
            OneTimeTokenHash = tokenHash,
            TokenSalt = salt,
            TokenCreatedAtUtc = DateTime.UtcNow,
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };

        try
        {
            _context.StoredFiles.Add(storedFile);
            await _context.SaveChangesAsync();

            _logger.LogInformation("File upload completed for user {UserId}, file {FileId}, size {SizeBytes} bytes", 
                userId, storedFile.Id, sizeBytes);

            return (storedFile, tokenPlain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file metadata for user {UserId}", userId);
            
            // Clean up the physical file if database save failed
            try
            {
                await _storageService.DeleteAsync(storagePath);
            }
            catch (Exception deleteEx)
            {
                _logger.LogWarning(deleteEx, "Failed to clean up file {StoragePath} after database save failure", storagePath);
            }
            
            throw;
        }
    }

    public async Task<DownloadResult> TryConsumeTokenAndDownloadAsync(Guid fileId, string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return DownloadResult.NotFound();
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Find the file record
            var storedFile = await _context.StoredFiles
                .Where(f => f.Id == fileId && f.DeletedAtUtc == null)
                .FirstOrDefaultAsync();

            if (storedFile == null)
            {
                _logger.LogWarning("Download attempt for non-existent or deleted file {FileId}", fileId);
                return DownloadResult.NotFound();
            }

            // Check if file has expired
            if (storedFile.ExpiresAtUtc.HasValue && storedFile.ExpiresAtUtc.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("Download attempt for expired file {FileId}", fileId);
                return DownloadResult.Expired();
            }

            // Check if token has already been used
            if (storedFile.TokenUsedAtUtc.HasValue)
            {
                _logger.LogWarning("Download attempt for already used token, file {FileId}", fileId);
                return DownloadResult.AlreadyUsed();
            }

            // Verify token
            if (!_tokenService.VerifyToken(token, storedFile.OneTimeTokenHash, storedFile.TokenSalt))
            {
                _logger.LogWarning("Download attempt with invalid token for file {FileId}", fileId);
                return DownloadResult.NotFound();
            }

            // Mark token as used and file as deleted (conditional update for concurrency safety)
            var now = DateTime.UtcNow;
            var originalConcurrencyStamp = storedFile.ConcurrencyStamp;
            storedFile.TokenUsedAtUtc = now;
            storedFile.DeletedAtUtc = now;
            storedFile.ConcurrencyStamp = Guid.NewGuid().ToString();

            var affectedRows = await _context.Database.ExecuteSqlRawAsync(
                @"UPDATE StoredFiles 
                  SET TokenUsedAtUtc = {0}, DeletedAtUtc = {1}, ConcurrencyStamp = {2}
                  WHERE Id = {3} AND TokenUsedAtUtc IS NULL AND DeletedAtUtc IS NULL AND ConcurrencyStamp = {4}",
                now, now, storedFile.ConcurrencyStamp, fileId, originalConcurrencyStamp);

            if (affectedRows != 1)
            {
                _logger.LogWarning("Concurrent download attempt detected for file {FileId}", fileId);
                return DownloadResult.AlreadyUsed();
            }

            // Open file stream
            Stream fileStream;
            try
            {
                fileStream = await _storageService.OpenReadAsync(storedFile.StoragePath);
            }
            catch (FileNotFoundException)
            {
                _logger.LogError("Physical file not found for file {FileId} at path {StoragePath}", fileId, storedFile.StoragePath);
                return DownloadResult.NotFound();
            }

            await transaction.CommitAsync();

            _logger.LogInformation("Successful one-time download for file {FileId} by user {UserId}", fileId, storedFile.OwnerId);

            // Schedule physical file deletion (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _storageService.DeleteAsync(storedFile.StoragePath);
                    _logger.LogInformation("Physical file deleted for {FileId}", fileId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete physical file for {FileId} at {StoragePath}", fileId, storedFile.StoragePath);
                }
            });

            return DownloadResult.Success(
                fileStream, 
                storedFile.OriginalFileName, 
                storedFile.ContentType, 
                storedFile.SizeBytes);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during download attempt for file {FileId}", fileId);
            throw;
        }
    }
}