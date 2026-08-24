using PetHub.API.DTOs.User;
using PetHub.API.Models;

namespace PetHub.API.Mappings;

public static class UserMappingExtensions
{
    public static PublicUserResponseDto ToPublicResponseDto(this User user)
    {
        return new PublicUserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            ProfilePictureUrl = user.ProfilePictureUrl,
            City = user.City,
            State = user.State,
            AccountType = user.AccountType,
            Description = user.Description,
            Cnpj = user.Cnpj,
        };
    }

    public static UserResponseDto ToResponseDto(this User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            EmailVerified = user.EmailVerified,
            ProfilePictureUrl = user.ProfilePictureUrl,
            AccountType = user.AccountType,
            Cnpj = user.Cnpj,
            Description = user.Description,
            PhoneNumber = user.PhoneNumber,
            City = user.City,
            State = user.State,
            Neighborhood = user.Neighborhood,
            Street = user.Street,
            StreetNumber = user.StreetNumber,
        };
    }
}
