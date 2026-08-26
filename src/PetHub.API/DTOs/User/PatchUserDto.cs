using System.ComponentModel.DataAnnotations;
using PetHub.API.Enums;
using PetHub.API.Utils;

namespace PetHub.API.DTOs.User;

public class PatchUserDto : IValidatableObject
{
    // All fields are Nullable (?).
    // If null, we ignore. If has value, we update.

    [StringLength(30, ErrorMessage = "Name cannot exceed 30 characters.")]
    public string? Name { get; set; }

    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(20, MinimumLength = 6)]
    public string? Password { get; set; }

    [StringLength(20, MinimumLength = 6)]
    public string? CurrentPassword { get; set; }

    [Phone]
    [StringLength(15, MinimumLength = 10)]
    [RegularExpression(@"^\d+$", ErrorMessage = "Phone number must contain only numbers.")]
    public string? PhoneNumber { get; set; }

    [StringLength(8, MinimumLength = 8)]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "ZipCode must be exactly 8 numbers.")]
    public string? ZipCode { get; set; }

    [StringLength(2, MinimumLength = 2)]
    public string? State { get; set; }

    [StringLength(50)]
    public string? City { get; set; }

    [StringLength(50)]
    public string? Neighborhood { get; set; }

    [StringLength(100)]
    public string? Street { get; set; }

    [StringLength(10)]
    public string? StreetNumber { get; set; }

    public UserType? AccountType { get; set; }

    [StringLength(18)]
    public string? Cnpj { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Password) && string.IsNullOrWhiteSpace(CurrentPassword))
        {
            yield return new ValidationResult(
                "Current password is required to change the password.",
                [nameof(CurrentPassword)]
            );
        }

        if (
            !string.IsNullOrWhiteSpace(Password)
            && !string.IsNullOrWhiteSpace(CurrentPassword)
            && Password == CurrentPassword
        )
        {
            yield return new ValidationResult(
                "New password must be different from the current password.",
                [nameof(Password)]
            );
        }

        if (!string.IsNullOrWhiteSpace(Cnpj) && !CnpjHelper.IsValid(Cnpj))
        {
            yield return new ValidationResult("A valid CNPJ is required.", [nameof(Cnpj)]);
        }

        if (AccountType == UserType.Person && !string.IsNullOrWhiteSpace(Cnpj))
        {
            yield return new ValidationResult(
                "CNPJ is only allowed for shelter accounts.",
                [nameof(Cnpj)]
            );
        }

        if (AccountType == UserType.Shelter && Cnpj != null && !CnpjHelper.IsValid(Cnpj))
        {
            yield return new ValidationResult(
                "A valid CNPJ is required for shelter accounts.",
                [nameof(Cnpj)]
            );
        }
    }
}
