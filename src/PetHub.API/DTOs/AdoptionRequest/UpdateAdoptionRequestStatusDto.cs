using System.ComponentModel.DataAnnotations;
using PetHub.API.Enums;

namespace PetHub.API.DTOs.AdoptionRequest;

public class UpdateAdoptionRequestStatusDto
{
    [Required(ErrorMessage = "Status is required")]
    public AdoptionStatus Status { get; set; }
}
