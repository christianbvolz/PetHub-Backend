using System.ComponentModel.DataAnnotations;

namespace PetHub.API.DTOs.User;

public class PatchUserDto
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
}
