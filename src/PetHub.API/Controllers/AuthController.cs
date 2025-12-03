using Microsoft.AspNetCore.Mvc;
using PetHub.API.Common;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.User;
using PetHub.API.Mappings;
using PetHub.API.Services;

namespace PetHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IUserRepository userRepository, IJwtService jwtService)
    : ApiControllerBase
{
    /// <summary>
    /// Registers a new user in the system
    /// </summary>
    /// <param name="dto">New user data including name, email, password, phone and address</param>
    /// <returns>JWT token and created user data</returns>
    /// <response code="200">User registered successfully</response>
    /// <response code="400">Invalid data (email already registered or validation failed)</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Register(CreateUserDto dto)
    {
        try
        {
            var user = await userRepository.CreateAsync(dto);
            var token = jwtService.GenerateToken(user.Id, user.Email);

            var loginResponse = new LoginResponseDto { Token = token, User = user.ToResponseDto() };

            return Success(loginResponse);
        }
        catch (InvalidOperationException ex)
        {
            return Error(ex.Message);
        }
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    /// <param name="dto">Login credentials (email and password)</param>
    /// <returns>JWT token and authenticated user data</returns>
    /// <response code="200">Login successful</response>
    /// <response code="401">Invalid email or password</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(LoginDto dto)
    {
        var user = await userRepository.AuthenticateAsync(dto.Email, dto.Password);

        if (user == null)
        {
            return Unauthorized("Invalid email or password.");
        }

        var token = jwtService.GenerateToken(user.Id, user.Email);
        var loginResponse = new LoginResponseDto { Token = token, User = user.ToResponseDto() };

        return Success(loginResponse);
    }
}
