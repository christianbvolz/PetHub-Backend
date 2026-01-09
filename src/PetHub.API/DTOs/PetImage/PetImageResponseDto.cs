namespace PetHub.API.DTOs.PetImage;

public class PetImageResponseDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int PetId { get; set; }
}
