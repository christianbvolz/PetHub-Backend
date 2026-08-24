using System.ComponentModel.DataAnnotations;
using PetHub.API.Enums;
using PetHub.API.Utils;

namespace PetHub.API.DTOs.User;

public class CreateUserDto : IValidatableObject
{
    [Required]
    [StringLength(30, ErrorMessage = "Name cannot exceed 30 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(15, MinimumLength = 10)]
    [RegularExpression(@"^\d+$", ErrorMessage = "Phone number must contain only numbers.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(8, MinimumLength = 8)]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "ZipCode must be exactly 8 numbers (no dashes).")]
    public string ZipCode { get; set; } = string.Empty;

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string State { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Neighborhood { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string StreetNumber { get; set; } = string.Empty;

    public UserType AccountType { get; set; } = UserType.Person;

    [StringLength(18)]
    public string? Cnpj { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AccountType == UserType.Shelter)
        {
            if (!CnpjHelper.IsValid(Cnpj))
            {
                yield return new ValidationResult(
                    "A valid CNPJ is required for shelter accounts.",
                    [nameof(Cnpj)]
                );
            }
        }
        else if (!string.IsNullOrWhiteSpace(Cnpj))
        {
            yield return new ValidationResult(
                "CNPJ is only allowed for shelter accounts.",
                [nameof(Cnpj)]
            );
        }
    }
}
