using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OneTimeShare.Web.Models;
using OneTimeShare.Web.Models.Options;
using OneTimeShare.Web.Models.ViewModels;
using OneTimeShare.Web.Services;
using System.Security.Claims;

namespace OneTimeShare.Web.Controllers;

[Authorize]
public class FilesController : Controller
{
    private readonly IFileService _fileService;
    private readonly OneTimeShare.Web.Models.Options.FileOptions _fileOptions;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IFileService fileService, 
        IOptions<OneTimeShare.Web.Models.Options.FileOptions> fileOptions,
        ILogger<FilesController> logger)
    {
        _fileService = fileService;
        _fileOptions = fileOptions.Value;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Upload()
    {
        var model = new UploadViewModel
        {
            MaxUploadBytes = _fileOptions.MaxUploadBytes
        };
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(UploadViewModel model)
    {
        model.MaxUploadBytes = _fileOptions.MaxUploadBytes;
        
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.File == null || model.File.Length == 0)
        {
            ModelState.AddModelError(nameof(model.File), "Please select a file to upload.");
            return View(model);
        }

        if (model.File.Length > _fileOptions.MaxUploadBytes)
        {
            ModelState.AddModelError(nameof(model.File), $"File size exceeds the maximum allowed size of {model.MaxUploadSizeDisplay}.");
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Upload attempt by user without valid ID claim");
            return Unauthorized();
        }

        try
        {
            using var fileStream = model.File.OpenReadStream();
            var (storedFile, oneTimeToken) = await _fileService.CreateUploadRecordAsync(
                userId,
                model.File.FileName,
                model.File.ContentType,
                fileStream);

            var baseUrl = Environment.GetEnvironmentVariable("APP_BASE_URL") ?? 
                          $"{Request.Scheme}://{Request.Host}";
            
            var downloadUrl = $"{baseUrl}/d/{storedFile.Id}?t={oneTimeToken}";

            var successModel = new UploadSuccessViewModel
            {
                FileId = storedFile.Id,
                OriginalFileName = storedFile.OriginalFileName,
                SizeBytes = storedFile.SizeBytes,
                OneTimeToken = oneTimeToken,
                DownloadUrl = downloadUrl,
                ExpiresAtUtc = storedFile.ExpiresAtUtc ?? DateTime.UtcNow.AddDays(_fileOptions.RetentionDays)
            };

            _logger.LogInformation("File uploaded successfully by user {UserId}, file {FileId}", userId, storedFile.Id);

            return View("Success", successModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for user {UserId}", userId);
            ModelState.AddModelError("", "An error occurred while uploading the file. Please try again.");
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Success(Guid id)
    {
        // This action is mainly for direct navigation to success page
        // The actual success view is typically shown after upload
        return RedirectToAction("Upload");
    }
}

[AllowAnonymous]
public class DownloadController : Controller
{
    private readonly IFileService _fileService;
    private readonly ILogger<DownloadController> _logger;

    public DownloadController(IFileService fileService, ILogger<DownloadController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    [HttpGet("/d/{fileId}")]
    public async Task<IActionResult> Download(Guid fileId, [FromQuery] string t)
    {
        if (string.IsNullOrEmpty(t))
        {
            _logger.LogWarning("Download attempt for file {FileId} without token", fileId);
            return NotFound();
        }

        try
        {
            var result = await _fileService.TryConsumeTokenAndDownloadAsync(fileId, t);

            switch (result.ResultType)
            {
                case DownloadResultType.Success:
                    _logger.LogInformation("Successful download for file {FileId}", fileId);
                    
                    // Set security headers
                    Response.Headers.Add("X-Content-Type-Options", "nosniff");
                    Response.Headers.Add("Cache-Control", "no-store");
                    
                    return File(
                        result.FileStream!,
                        result.ContentType ?? "application/octet-stream",
                        result.SafeFileName,
                        enableRangeProcessing: true);

                case DownloadResultType.NotFound:
                    _logger.LogWarning("Download attempt for non-existent file {FileId}", fileId);
                    return NotFound();

                case DownloadResultType.AlreadyUsed:
                    _logger.LogWarning("Download attempt for already used token, file {FileId}", fileId);
                    return StatusCode(410, "This download link has already been used and is no longer valid.");

                case DownloadResultType.Expired:
                    _logger.LogWarning("Download attempt for expired file {FileId}", fileId);
                    return StatusCode(410, "This download link has expired and is no longer valid.");

                default:
                    _logger.LogError("Unexpected download result type {ResultType} for file {FileId}", result.ResultType, fileId);
                    return StatusCode(500, "An error occurred while processing the download.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during download attempt for file {FileId}", fileId);
            return StatusCode(500, "An error occurred while processing the download.");
        }
    }
}