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
public class UsersController(
    IUserRepository userRepository,
    IAuthLifecycleService authLifecycleService
) : ApiControllerBase
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
    /// Retrieves a sanitized public profile for a person or shelter
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Public profile data (name, photo, city/state, account type). Contact details are omitted.</returns>
    /// <response code="200">Profile found successfully</response>
    /// <response code="404">User not found</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PublicUserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PublicUserResponseDto>>> GetUser(Guid id)
    {
        var user = await userRepository.GetByIdAsync(id);

        if (user == null)
        {
            return NotFound("User not found.");
        }

        return Success(user.ToPublicResponseDto());
    }

    /// <summary>
    /// Updates the authenticated user's profile (partial update)
    /// </summary>
    /// <param name="dto">Data to be updated. Supports partial update of name, email, password, phone, address, account type, CNPJ and description</param>
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

        var currentUser = await userRepository.GetByIdAsync(userId);
        if (currentUser == null)
        {
            return NotFound("User not found.");
        }

        bool emailChanged =
            dto.Email != null
            && !string.Equals(dto.Email, currentUser.Email, StringComparison.Ordinal);
        bool requiresReauth =
            !string.IsNullOrEmpty(dto.Email) || !string.IsNullOrEmpty(dto.Password);

        try
        {
            var success = await userRepository.UpdateAsync(userId, dto);

            if (!success)
            {
                return NotFound("User not found.");
            }

            if (emailChanged)
            {
                var updatedUser = await userRepository.GetByIdAsync(userId);
                if (updatedUser != null)
                {
                    await authLifecycleService.SendVerificationEmailAsync(updatedUser);
                }
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

    /// <summary>
    /// Deletes the authenticated user's account
    /// </summary>
    /// <returns>Indicates deletion success</returns>
    /// <response code="200">User deleted successfully</response>
    /// <response code="401">User not authenticated or invalid token</response>
    /// <response code="404">User not found</response>
    [HttpDelete("me")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCurrentUser()
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result != null)
            return userIdResult.Result;

        var userId = userIdResult.Value;

        var success = await userRepository.DeleteAsync(userId);

        if (!success)
        {
            return NotFound("User not found.");
        }

        return Success(new { }, "User deleted successfully.");
    }
}
