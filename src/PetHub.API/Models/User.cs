using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PetHub.API.Enums;

namespace PetHub.API.Models;

public class User
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    public bool EmailVerified { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ProfilePictureUrl { get; set; } = string.Empty;

    public UserType AccountType { get; set; } = UserType.Person;

    [MaxLength(14)]
    public string Cnpj { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(8)]
    public string ZipCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(2)]
    public string State { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Neighborhood { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string StreetNumber { get; set; } = string.Empty;

    [JsonIgnore]
    public List<Pet> Pets { get; set; } = [];

    [JsonIgnore]
    public List<RefreshToken> RefreshTokens { get; set; } = new();

    [JsonIgnore]
    public List<AuthToken> AuthTokens { get; set; } = [];
}
