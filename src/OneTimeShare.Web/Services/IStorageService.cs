namespace OneTimeShare.Web.Services;

public interface IStorageService
{
    Task<(string relativePath, long sizeBytes)> SaveAsync(Stream fileStream, string extension);
    Task<Stream> OpenReadAsync(string relativePath);
    Task DeleteAsync(string relativePath);
    bool FileExists(string relativePath);
}