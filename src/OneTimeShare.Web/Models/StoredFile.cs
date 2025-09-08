namespace OneTimeShare.Web.Models;

public class StoredFile
{
    public Guid Id { get; set; }
    public string OwnerId { get; set; } = string.Empty; // FK to UserAccount.Id
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty; // relative path from storage root
    public DateTime UploadAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; } // set when one-time download is completed or during cleanup
    public DateTime? ExpiresAtUtc { get; set; } // set to UploadAtUtc + retention days
    public string OneTimeTokenHash { get; set; } = string.Empty; // hash of token (never store raw token)
    public string TokenSalt { get; set; } = string.Empty; // salt for token hash
    public DateTime TokenCreatedAtUtc { get; set; }
    public DateTime? TokenUsedAtUtc { get; set; } // when first successful download occurs
    public string ConcurrencyStamp { get; set; } = string.Empty; // for optimistic concurrency
    
    // Navigation properties
    public UserAccount Owner { get; set; } = null!;
}