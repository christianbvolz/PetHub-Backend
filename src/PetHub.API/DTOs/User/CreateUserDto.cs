using System.ComponentModel.DataAnnotations;

namespace PetHub.API.DTOs.User;

public class CreateUserDto
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
}
