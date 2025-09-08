using OneTimeShare.Web.Models;

namespace OneTimeShare.Web.Services;

public interface IFileService
{
    Task<(StoredFile storedFile, string oneTimeToken)> CreateUploadRecordAsync(
        string userId, 
        string originalFileName, 
        string contentType, 
        Stream fileStream);
    
    Task<DownloadResult> TryConsumeTokenAndDownloadAsync(Guid fileId, string token);
}