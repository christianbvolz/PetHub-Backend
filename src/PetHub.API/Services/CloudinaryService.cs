using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using PetHub.API.Configuration;

namespace PetHub.API.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary? _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public CloudinaryService(IOptions<CloudinarySettings> config, ILogger<CloudinaryService> logger)
    {
        _logger = logger;
        var cfg = config.Value;

        if (string.IsNullOrWhiteSpace(cfg.CloudName))
        {
            _logger.LogWarning(
                "Cloudinary is not configured (missing CloudName). CloudinaryService will be disabled."
            );
            _cloudinary = null;
            return;
        }

        var account = new Account(cfg.CloudName, cfg.ApiKey, cfg.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folderName = "pets")
    {
        if (_cloudinary == null)
            throw new InvalidOperationException("Cloudinary is not configured.");
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or null");
        }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            throw new ArgumentException(
                $"File type {extension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}"
            );
        }

        // Validate file size (max 5MB)
        const int maxFileSizeInBytes = 5 * 1024 * 1024;
        if (file.Length > maxFileSizeInBytes)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size of 5MB");
        }

        try
        {
            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folderName,
                Transformation = new Transformation()
                    .Width(1200)
                    .Height(1200)
                    .Crop("limit")
                    .Quality("auto"),
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {Error}", uploadResult.Error.Message);
                throw new Exception($"Image upload failed: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image to Cloudinary");
            throw;
        }
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        if (_cloudinary == null)
            throw new InvalidOperationException("Cloudinary is not configured.");

        try
        {
            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);

            if (result.Error != null)
            {
                _logger.LogError("Cloudinary deletion error: {Error}", result.Error.Message);
                return false;
            }

            return result.Result == "ok";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image from Cloudinary");
            return false;
        }
    }
}
