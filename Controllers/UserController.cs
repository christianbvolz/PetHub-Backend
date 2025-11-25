using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pethub.Data;
using pethub.DTOs;
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
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await context.Users.ToListAsync();
    }

    // POST: api/users
    // Creates a new user with secure password hashing
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(CreateUserDto dto)
    {
        // 1. Validation (Business Logic must still be handled explicitly)
        // We check if the email exists because this is a "logical" error, not a "system" error.
        if (await context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest("Email already registered.");
        }

        // 2. Mapping: Convert DTO to User Entity
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

        return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, user);
    }
}
