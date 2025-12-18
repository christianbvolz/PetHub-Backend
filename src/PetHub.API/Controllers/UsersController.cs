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
    /// <summary>
    /// Retrieves the authenticated user's profile
    /// </summary>
    /// <returns>Authenticated user profile data</returns>
    /// <response code="200">Profile found successfully</response>
    /// <response code="401">User not authenticated or invalid token</response>
    /// <response code="404">User not found</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserResponseDto>>> GetCurrentUser()
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result != null) // Returns 401 Unauthorized if token is invalid
            return userIdResult.Result;

        var userId = userIdResult.Value; // Extracts Guid from successful result

        var user = await userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound("User not found.");
        }

        return Success(user.ToResponseDto());
    }

    /// <summary>
    /// Updates the authenticated user's profile (partial update)
    /// </summary>
    /// <param name="dto">Data to be updated. Supports partial update of name, email, password, phone, address and profile picture</param>
    /// <returns>Indicates if the update was successful and if re-authentication is required</returns>
    /// <response code="200">Profile updated successfully</response>
    /// <response code="400">Invalid data (email already registered)</response>
    /// <response code="401">User not authenticated or invalid token</response>
    /// <response code="404">User not found</response>
    /// <remarks>
    /// If email or password are changed, the user must login again.
    /// </remarks>
    [HttpPatch("me")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> PatchCurrentUser(PatchUserDto dto)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result != null) // Returns 401 Unauthorized if token is invalid
            return userIdResult.Result;

        var userId = userIdResult.Value; // Extracts Guid from successful result

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
