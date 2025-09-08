namespace OneTimeShare.Web.Models.ViewModels;

public class UploadSuccessViewModel
{
    public Guid FileId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string OneTimeToken { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    
    public string SizeDisplay => FormatBytes(SizeBytes);
    
    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }
}