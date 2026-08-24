using PetHub.API.Enums;

namespace PetHub.API.DTOs.User;

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public UserType AccountType { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string StreetNumber { get; set; } = string.Empty;
}
