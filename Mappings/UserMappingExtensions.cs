using pethub.DTOs.User;
using pethub.Models;

namespace pethub.Mappings;

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
