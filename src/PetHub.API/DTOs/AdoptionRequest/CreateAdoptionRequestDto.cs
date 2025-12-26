using System.ComponentModel.DataAnnotations;

namespace PetHub.API.DTOs.AdoptionRequest;

public class CreateAdoptionRequestDto
{
    [Required(ErrorMessage = "PetId is required")]
    public int PetId { get; set; }

    [Required(ErrorMessage = "Message is required")]
    [StringLength(
        1000,
        MinimumLength = 10,
        ErrorMessage = "Message must be between 10 and 1000 characters"
    )]
    public string Message { get; set; } = string.Empty;
}
