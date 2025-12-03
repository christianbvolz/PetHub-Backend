using PetHub.API.Enums;

namespace PetHub.API.DTOs.Adoption;

public class AdoptionRequestResponseDto
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public AdoptionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    // Adopter info
    public Guid AdopterId { get; set; }
    public string AdopterName { get; set; } = string.Empty;
    public string? AdopterEmail { get; set; }
    public string? AdopterPhone { get; set; }
    public string? AdopterPhotoUrl { get; set; }

    // Pet info (optional, for adopter's view)
    public int? PetId { get; set; }
    public string? PetName { get; set; }
}
