using PetHub.API.Enums;

namespace PetHub.API.DTOs.User;

public class PublicUserResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public UserType AccountType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
}
