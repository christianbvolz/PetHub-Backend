using PetHub.API.DTOs.Pet;
using PetHub.API.DTOs.User;
using PetHub.API.Enums;

namespace PetHub.API.DTOs.AdoptionRequest;

public class AdoptionRequestResponseDto
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public Guid AdopterId { get; set; }
    public string Message { get; set; } = string.Empty;
    public AdoptionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public PetResponseDto? Pet { get; set; }
    public UserResponseDto? Adopter { get; set; }
}
