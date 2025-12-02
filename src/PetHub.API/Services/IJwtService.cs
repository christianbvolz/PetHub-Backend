namespace PetHub.API.Services;

public interface IJwtService
{
    string GenerateToken(Guid userId, string email);
}
