using PetHub.API.DTOs.User;
using PetHub.API.Models;

namespace PetHub.API.Services;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(CreateUserDto dto);
    Task<bool> UpdateAsync(Guid id, PatchUserDto dto);
    Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null);
    Task<User?> AuthenticateAsync(string email, string password);
    Task<bool> DeleteAsync(Guid id);
}
