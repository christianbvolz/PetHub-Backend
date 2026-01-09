namespace PetHub.API.Services;

public interface ICloudinaryService
{
    Task<string> UploadImageAsync(IFormFile file, string folderName = "pets");
    Task<bool> DeleteImageAsync(string publicId);
}
