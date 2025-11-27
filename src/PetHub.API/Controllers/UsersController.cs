using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pethub.Data;
using pethub.DTOs.User;
using pethub.Mappings;
using pethub.Models;
using pethub.Utils;

namespace pethub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(AppDbContext context) : ControllerBase
{
    // GET: api/users
    // Retrieves all registered users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAllUsers()
    {
        var users = await context.Users.ToListAsync();

        // Use the extension method to map the list cleanly
        return users.Select(u => u.ToResponseDto()).ToList();
    }

    // GET: api/users/5
    // Retrieves a specific user by ID
    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponseDto>> GetUser(int id)
    {
        var user = await context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound($"User with ID {id} not found.");
        }

        // Use the extension method to map the single object
        return user.ToResponseDto();
    }

    // POST: api/users
    // Creates a new user
    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> CreateUser(CreateUserDto dto)
    {
        // 1. Validation
        if (await context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest("Email already registered.");
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

        // This converts the saved User entity back to a safe DTO
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user.ToResponseDto());
    }

    // PATCH: api/users/{id}
    // Universally updates user. Supports partial updates.
    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchUser(int id, PatchUserDto dto)
    {
        var user = await context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound($"User with ID {id} not found.");
        }

        // Conditional Updates
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

        if (dto.Email != null && dto.Email != user.Email)
        {
            if (await context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id))
            {
                return BadRequest("Email already in use by another account.");
            }
            user.Email = dto.Email;
        }

        if (!string.IsNullOrEmpty(dto.Password))
        {
            user.PasswordHash = PasswordHelper.HashPassword(dto.Password);
        }

        await context.SaveChangesAsync();

        return Ok("User updated successfully.");
    }
}
