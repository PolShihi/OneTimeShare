namespace OneTimeShare.Web.Models.Options;

public class FileOptions
{
    public long MaxUploadBytes { get; set; } = 104857600; 
    public int RetentionDays { get; set; } = 30;
}