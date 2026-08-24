using System.ComponentModel.DataAnnotations;

namespace PetHub.API.DTOs.Chat;

public class CreateConversationDto : IValidatableObject
{
    /// <summary>
    /// Pet to talk about. Required when <see cref="AdoptionRequestId"/> is not provided.
    /// </summary>
    public int? PetId { get; set; }

    /// <summary>
    /// Adoption request that should be linked to this conversation.
    /// When set, the thread is between the adopter and the pet owner.
    /// </summary>
    public int? AdoptionRequestId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PetId.HasValue && !AdoptionRequestId.HasValue)
        {
            yield return new ValidationResult(
                "Either PetId or AdoptionRequestId is required.",
                [nameof(PetId), nameof(AdoptionRequestId)]
            );
        }
    }
}
