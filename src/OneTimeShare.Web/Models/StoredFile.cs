namespace OneTimeShare.Web.Models;

public class StoredFile
{
    public Guid Id { get; set; }
    public string OwnerId { get; set; } = string.Empty; 
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty; 
    public DateTime UploadAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; } 
    public DateTime? ExpiresAtUtc { get; set; } 
    public string OneTimeTokenHash { get; set; } = string.Empty; 
    public string TokenSalt { get; set; } = string.Empty; 
    public DateTime TokenCreatedAtUtc { get; set; }
    public DateTime? TokenUsedAtUtc { get; set; } 
    public string ConcurrencyStamp { get; set; } = string.Empty; 
    
    
    public UserAccount Owner { get; set; } = null!;
}