namespace OneTimeShare.Web.Models;

public class UserAccount
{
    public string Id { get; set; } = string.Empty; // Google subject (sub claim)
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastLoginAtUtc { get; set; }
    
    // Navigation properties
    public ICollection<StoredFile> StoredFiles { get; set; } = new List<StoredFile>();
}