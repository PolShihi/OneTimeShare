namespace OneTimeShare.Web.Models;

public enum DownloadResultType
{
    Success,
    NotFound,
    AlreadyUsed,
    Expired
}

public class DownloadResult
{
    public DownloadResultType ResultType { get; set; }
    public Stream? FileStream { get; set; }
    public string? SafeFileName { get; set; }
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }

    public static DownloadResult Success(Stream fileStream, string safeFileName, string contentType, long sizeBytes)
    {
        return new DownloadResult
        {
            ResultType = DownloadResultType.Success,
            FileStream = fileStream,
            SafeFileName = safeFileName,
            ContentType = contentType,
            SizeBytes = sizeBytes
        };
    }

    public static DownloadResult NotFound()
    {
        return new DownloadResult { ResultType = DownloadResultType.NotFound };
    }

    public static DownloadResult AlreadyUsed()
    {
        return new DownloadResult { ResultType = DownloadResultType.AlreadyUsed };
    }

    public static DownloadResult Expired()
    {
        return new DownloadResult { ResultType = DownloadResultType.Expired };
    }
}