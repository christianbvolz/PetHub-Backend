using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;
using PetHub.API.DTOs.User;
using PetHub.API.Enums;
using PetHub.API.Models;
using PetHub.API.Utils;

namespace PetHub.API.Services;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await context.Users.ToListAsync();
    }

    public async Task<User?> GetByIdAsync(Guid id)
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

        var cnpj = ResolveCnpj(dto.AccountType, dto.Cnpj);
        if (dto.AccountType == UserType.Shelter && await CnpjExistsAsync(cnpj))
        {
            throw new InvalidOperationException("CNPJ already registered.");
        }

        var user = new User
        {
            Id = UuidHelper.NewId(),
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
            AccountType = dto.AccountType,
            Cnpj = cnpj,
            Description = dto.Description?.Trim() ?? string.Empty,
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    public async Task<bool> UpdateAsync(Guid id, PatchUserDto dto)
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
            user.EmailVerified = false;
            user.EmailVerifiedAt = null;
        }

        // Business logic: hash password when changing
        if (!string.IsNullOrEmpty(dto.Password))
        {
            if (
                string.IsNullOrEmpty(dto.CurrentPassword)
                || !PasswordHelper.VerifyPassword(dto.CurrentPassword, user.PasswordHash)
            )
            {
                throw new InvalidOperationException("Current password is incorrect.");
            }

            if (PasswordHelper.VerifyPassword(dto.Password, user.PasswordHash))
            {
                throw new InvalidOperationException(
                    "New password must be different from the current password."
                );
            }

            user.PasswordHash = PasswordHelper.HashPassword(dto.Password);
        }

        if (dto.AccountType.HasValue)
            user.AccountType = dto.AccountType.Value;

        if (dto.Description != null)
            user.Description = dto.Description.Trim();

        if (dto.Cnpj != null)
            user.Cnpj = CnpjHelper.Normalize(dto.Cnpj);

        if (user.AccountType == UserType.Person)
        {
            if (dto.Cnpj != null && !string.IsNullOrWhiteSpace(dto.Cnpj))
            {
                throw new InvalidOperationException("CNPJ is only allowed for shelter accounts.");
            }

            user.Cnpj = string.Empty;
        }
        else
        {
            if (!CnpjHelper.IsValid(user.Cnpj))
            {
                throw new InvalidOperationException(
                    "A valid CNPJ is required for shelter accounts."
                );
            }

            if (await CnpjExistsAsync(user.Cnpj, id))
            {
                throw new InvalidOperationException("CNPJ already registered.");
            }
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null)
    {
        if (excludeUserId.HasValue)
        {
            return await context.Users.AnyAsync(u =>
                u.Email == email && u.Id != excludeUserId.Value
            );
        }

        return await context.Users.AnyAsync(u => u.Email == email);
    }

    private async Task<bool> CnpjExistsAsync(string cnpj, Guid? excludeUserId = null)
    {
        if (string.IsNullOrEmpty(cnpj))
            return false;

        if (excludeUserId.HasValue)
        {
            return await context.Users.AnyAsync(u =>
                u.Cnpj == cnpj && u.Id != excludeUserId.Value
            );
        }

        return await context.Users.AnyAsync(u => u.Cnpj == cnpj);
    }

    private static string ResolveCnpj(UserType accountType, string? cnpj)
    {
        if (accountType != UserType.Shelter)
            return string.Empty;

        var normalized = CnpjHelper.Normalize(cnpj);
        if (!CnpjHelper.IsValid(normalized))
        {
            throw new InvalidOperationException("A valid CNPJ is required for shelter accounts.");
        }

        return normalized;
    }

    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        var user = await GetByEmailAsync(email);

        if (user == null)
        {
            return null;
        }

        // Verify password using BCrypt
        bool passwordValid = PasswordHelper.VerifyPassword(password, user.PasswordHash);

        return passwordValid ? user : null;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await GetByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        context.Users.Remove(user);
        await context.SaveChangesAsync();
        return true;
    }
}
