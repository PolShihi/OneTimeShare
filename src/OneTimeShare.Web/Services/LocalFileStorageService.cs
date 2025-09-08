using Microsoft.Extensions.Options;
using OneTimeShare.Web.Models.Options;

namespace OneTimeShare.Web.Services;

public class LocalFileStorageService : IStorageService
{
    private readonly string _storageRoot;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IOptions<StorageOptions> options, ILogger<LocalFileStorageService> logger)
    {
        _storageRoot = options.Value.StorageRoot;
        _logger = logger;
        
        
        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<(string relativePath, long sizeBytes)> SaveAsync(Stream fileStream, string extension)
    {
        var fileId = Guid.NewGuid();
        var fileName = $"{fileId}{extension}";
        var relativePath = Path.Combine("files", fileName);
        var fullPath = Path.Combine(_storageRoot, relativePath);
        
        
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            using var fileStreamOut = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            await fileStream.CopyToAsync(fileStreamOut);
            var sizeBytes = fileStreamOut.Length;
            
            _logger.LogInformation("File saved to {RelativePath}, size: {SizeBytes} bytes", relativePath, sizeBytes);
            return (relativePath, sizeBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save file to {RelativePath}", relativePath);
            
            
            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                }
                catch (Exception deleteEx)
                {
                    _logger.LogWarning(deleteEx, "Failed to clean up partial file {FullPath}", fullPath);
                }
            }
            
            throw;
        }
    }

    public async Task<Stream> OpenReadAsync(string relativePath)
    {
        var fullPath = Path.Combine(_storageRoot, relativePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {relativePath}");
        }

        try
        {
            return await Task.FromResult(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file for reading: {RelativePath}", relativePath);
            throw;
        }

    }

    public async Task DeleteAsync(string relativePath)
    {
        var fullPath = Path.Combine(_storageRoot, relativePath);
        
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("File deleted: {RelativePath}", relativePath);
            }
            else
            {
                _logger.LogWarning("Attempted to delete non-existent file: {RelativePath}", relativePath);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {RelativePath}", relativePath);
            throw;
        }
    }

    public bool FileExists(string relativePath)
    {
        var fullPath = Path.Combine(_storageRoot, relativePath);
        return File.Exists(fullPath);
    }
}