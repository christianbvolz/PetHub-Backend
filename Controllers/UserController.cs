using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pethub.Data;
using pethub.DTOs;
using pethub.Models;

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
    // Creates a new user.
    // Note: Address information (City, State, etc.) must be provided by the frontend.
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(CreateUserDto dto)
    {
        // 1. Basic Validation: Check if email is already in use
        if (await context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest("Email already registered.");
        }
        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            PhoneNumber = dto.PhoneNumber,
            ZipCode = dto.ZipCode,
            State = dto.State,
            City = dto.City,
            Neighborhood = dto.Neighborhood,
            Street = dto.Street,
            Number = dto.Number,
        };

        // 2. Save to Database
        // We assume the frontend sends the complete address based on the ZipCode
        context.Users.Add(user);

        await context.SaveChangesAsync();

        // Returns 201 Created with the location of the new resource
        return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, user);
    }
}
