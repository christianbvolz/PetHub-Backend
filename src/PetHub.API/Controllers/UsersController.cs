using Microsoft.AspNetCore.Mvc;
using PetHub.API.DTOs.User;
using PetHub.API.Mappings;
using PetHub.API.Services;

namespace PetHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserRepository userRepository) : ControllerBase
{
    // GET: api/users
    // Retrieves all registered users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAllUsers()
    {
        var users = await userRepository.GetAllAsync();
        return users.Select(u => u.ToResponseDto()).ToList();
    }

    // GET: api/users/5
    // Retrieves a specific user by ID
    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponseDto>> GetUser(int id)
    {
        var user = await userRepository.GetByIdAsync(id);

        if (user == null)
        {
            return NotFound($"User with ID {id} not found.");
        }

        return user.ToResponseDto();
    }

    // POST: api/users
    // Creates a new user
    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> CreateUser(CreateUserDto dto)
    {
        try
        {
            var user = await userRepository.CreateAsync(dto);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user.ToResponseDto());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // PATCH: api/users/{id}
    // Universally updates user. Supports partial updates.
    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchUser(int id, PatchUserDto dto)
    {
        try
        {
            var success = await userRepository.UpdateAsync(id, dto);

            if (!success)
            {
                return NotFound($"User with ID {id} not found.");
            }

            return Ok("User updated successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
