using Microsoft.AspNetCore.Mvc;
using PetHub.API.Common;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.User;
using PetHub.API.Mappings;
using PetHub.API.Services;

namespace PetHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IUserRepository userRepository,
    IJwtService jwtService,
    IRefreshTokenService refreshTokenService
) : ApiControllerBase
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

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var refresh = await refreshTokenService.CreateAsync(user.Id, ipAddress);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(14),
                Secure = true, // Only send over HTTPS
                SameSite = SameSiteMode.Lax // Or Strict, depending on your needs
            };
            Response.Cookies.Append("refreshToken", refresh, cookieOptions);

            var loginResponse = new LoginResponseDto
            {
                Token = token,
                User = user.ToResponseDto(),
                RefreshToken = null, // Do not send refresh token in the body
            };

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

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var refresh = await refreshTokenService.CreateAsync(user.Id, ipAddress);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(14),
            Secure = true,
            SameSite = SameSiteMode.Lax
        };
        Response.Cookies.Append("refreshToken", refresh, cookieOptions);

        var loginResponse = new LoginResponseDto
        {
            Token = token,
            User = user.ToResponseDto(),
            RefreshToken = null,
        };

        return Success(loginResponse);
    }

    /// <summary>
    /// Refreshes a user's session using a refresh token.
    /// </summary>
    /// <param name="dto">The refresh token DTO. If the token is not in the body, it's read from the `refreshToken` cookie.</param>
    /// <returns>A new JWT token and user data.</returns>
    /// <response code="200">Token refreshed successfully.</response>
    /// <response code="400">Invalid or expired refresh token.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Refresh(RefreshRequestDto? dto)
    {
        try
        {
            var incoming = dto?.RefreshToken ?? Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(incoming))
            {
                return Error("Refresh token is required.");
            }

            var existing = await refreshTokenService.GetByTokenAsync(incoming);
            if (existing == null || existing.User == null)
            {
                return Error("Invalid token.");
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var newRefresh = await refreshTokenService.RotateAsync(incoming, ipAddress);
            var newAccess = jwtService.GenerateToken(existing.User.Id, existing.User.Email);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(14),
                Secure = true,
                SameSite = SameSiteMode.Lax
            };
            Response.Cookies.Append("refreshToken", newRefresh, cookieOptions);

            var loginResponse = new LoginResponseDto
            {
                Token = newAccess,
                User = existing.User.ToResponseDto(),
                RefreshToken = null,
            };

            return Success(loginResponse);
        }
        catch (InvalidOperationException ex)
        {
            return Error(ex.Message);
        }
    }

    /// <summary>
    /// Revokes a refresh token, logging the user out from that session.
    /// </summary>
    /// <param name="dto">The refresh token DTO. If the token is not in the body, it's read from the `refreshToken` cookie.</param>
    /// <returns>A success message.</returns>
    /// <response code="200">Token revoked successfully.</response>
    /// <response code="400">Invalid refresh token.</response>
    [HttpPost("revoke")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<string>>> Revoke(RevokeRequestDto? dto)
    {
        var incoming = dto?.RefreshToken ?? Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(incoming))
        {
            return Error("Refresh token is required.");
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ok = await refreshTokenService.RevokeAsync(incoming, ipAddress);

        if (!ok)
        {
            return Error("Token not found or already revoked.");
        }

        // Also delete the cookie from the client
        var deleteOptions = new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax };
        Response.Cookies.Delete("refreshToken", deleteOptions);

        return Success("Token revoked successfully.");
    }
}
