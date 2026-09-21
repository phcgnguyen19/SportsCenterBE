using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCenterAPI.Models.DTOs.Auth;
using SportsCenterAPI.Models.DTOs.Login;
using SportsCenterAPI.Models.DTOs.Register;
using SportsCenterAPI.Models.DTOs.Response;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequestDTO request)
    {
        var response = await authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponse>.Ok(response, "Login successful"));
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterRequestDTO request)
    {
        var response = await authService.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<AuthResponse>.CreatedAt(response, "Registration successful"));
    }
}
