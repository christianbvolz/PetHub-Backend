using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetHub.API.Common;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.User;
using PetHub.API.Mappings;
using PetHub.API.Services;

namespace PetHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserRepository userRepository) : ApiControllerBase
{
    // GET: api/users/me
    // Retrieves the authenticated user's profile
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserResponseDto>>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("Invalid or missing user ID in token.");
        }

        var user = await userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound("User not found.");
        }

        return Success(user.ToResponseDto());
    }

    // PATCH: api/users/me
    // Updates the authenticated user's profile. Supports partial updates.
    // If email or password is changed, the user must re-authenticate.
    [HttpPatch("me")]
    public async Task<ActionResult<ApiResponse<object>>> PatchCurrentUser(PatchUserDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("Invalid or missing user ID in token.");
        }

        // Check if email or password is being changed (requires re-authentication)
        bool requiresReauth =
            !string.IsNullOrEmpty(dto.Email) || !string.IsNullOrEmpty(dto.Password);

        try
        {
            var success = await userRepository.UpdateAsync(userId, dto);

            if (!success)
            {
                return NotFound("User not found.");
            }

            var message = requiresReauth
                ? "User updated successfully. Please login again with your new credentials."
                : "User updated successfully.";

            return Success(new { requiresReauth }, message);
        }
        catch (InvalidOperationException ex)
        {
            return Error(ex.Message);
        }
    }
}
