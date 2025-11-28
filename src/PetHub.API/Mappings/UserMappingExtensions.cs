using PetHub.API.DTOs.User;
using PetHub.API.Models;

namespace PetHub.API.Mappings;

public static class UserMappingExtensions
{
    // Extension method: allows calling user.ToResponseDto()
    public static UserResponseDto ToResponseDto(this User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            ProfilePictureUrl = user.ProfilePictureUrl,
            PhoneNumber = user.PhoneNumber,
            City = user.City,
            State = user.State,
            Neighborhood = user.Neighborhood,
            Street = user.Street,
            StreetNumber = user.StreetNumber,
        };
    }
}
