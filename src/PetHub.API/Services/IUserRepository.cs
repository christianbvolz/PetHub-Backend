using PetHub.API.DTOs.User;
using PetHub.API.Models;

namespace PetHub.API.Services;

public interface IUserRepository
{
    public Task<IEnumerable<User>> GetAllAsync();
    public Task<User?> GetByIdAsync(int id);
    public Task<User?> GetByEmailAsync(string email);
    public Task<User> CreateAsync(CreateUserDto dto);
    public Task<bool> UpdateAsync(int id, PatchUserDto dto);
    public Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
}
