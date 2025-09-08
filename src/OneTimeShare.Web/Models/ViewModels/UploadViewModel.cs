using System.ComponentModel.DataAnnotations;

namespace OneTimeShare.Web.Models.ViewModels;

public class UploadViewModel
{
    [Required(ErrorMessage = "Please select a file to upload.")]
    [Display(Name = "File")]
    public IFormFile? File { get; set; }
    
    public long MaxUploadBytes { get; set; }
    
    public string MaxUploadSizeDisplay => FormatBytes(MaxUploadBytes);
    
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