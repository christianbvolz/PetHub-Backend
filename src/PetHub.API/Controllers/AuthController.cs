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
    // POST: api/auth/register
    [HttpPost("register")]
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

    // POST: api/auth/login
    [HttpPost("login")]
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
