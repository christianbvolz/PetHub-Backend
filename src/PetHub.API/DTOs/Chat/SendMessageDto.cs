using System.ComponentModel.DataAnnotations;

namespace PetHub.API.DTOs.Chat;

public class SendMessageDto
{
    [Required(ErrorMessage = "Content is required")]
    [StringLength(
        2000,
        MinimumLength = 1,
        ErrorMessage = "Message must be between 1 and 2000 characters"
    )]
    public string Content { get; set; } = string.Empty;
}
