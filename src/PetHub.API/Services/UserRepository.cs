using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;
using PetHub.API.DTOs.User;
using PetHub.API.Models;
using PetHub.API.Utils;

namespace PetHub.API.Services;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await context.Users.ToListAsync();
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await context.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User> CreateAsync(CreateUserDto dto)
    {
        // Business logic: validate email uniqueness
        if (await EmailExistsAsync(dto.Email))
        {
            throw new InvalidOperationException("Email already registered.");
        }

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = PasswordHelper.HashPassword(dto.Password),
            PhoneNumber = dto.PhoneNumber,
            ZipCode = dto.ZipCode,
            State = dto.State,
            City = dto.City,
            Neighborhood = dto.Neighborhood,
            Street = dto.Street,
            StreetNumber = dto.StreetNumber,
            ProfilePictureUrl = "",
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    public async Task<bool> UpdateAsync(int id, PatchUserDto dto)
    {
        var user = await GetByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        // Business logic: apply partial updates
        if (dto.Name != null)
            user.Name = dto.Name;

        if (dto.PhoneNumber != null)
            user.PhoneNumber = dto.PhoneNumber;

        if (dto.ZipCode != null)
            user.ZipCode = dto.ZipCode;

        if (dto.State != null)
            user.State = dto.State;

        if (dto.City != null)
            user.City = dto.City;

        if (dto.Neighborhood != null)
            user.Neighborhood = dto.Neighborhood;

        if (dto.Street != null)
            user.Street = dto.Street;

        if (dto.StreetNumber != null)
            user.StreetNumber = dto.StreetNumber;

        // Business logic: validate email uniqueness when changing
        if (dto.Email != null && dto.Email != user.Email)
        {
            if (await EmailExistsAsync(dto.Email, id))
            {
                throw new InvalidOperationException("Email already in use by another account.");
            }
            user.Email = dto.Email;
        }

        // Business logic: hash password when changing
        if (!string.IsNullOrEmpty(dto.Password))
        {
            user.PasswordHash = PasswordHelper.HashPassword(dto.Password);
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
    {
        if (excludeUserId.HasValue)
        {
            return await context.Users.AnyAsync(u =>
                u.Email == email && u.Id != excludeUserId.Value
            );
        }

        return await context.Users.AnyAsync(u => u.Email == email);
    }
}
