using Microsoft.AspNetCore.Mvc;
using SportsCenterAPI.DTOs.Auth;
using SportsCenterAPI.Services.Implement;

namespace SportsCenterAPI.Controllers;

/// <summary>
/// Controller xử lý xác thực: đăng nhập, đăng ký
/// Authentication controller: login and registration endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Đăng nhập — POST /api/auth/login
    /// </summary>
    /// <param name="request">Email và mật khẩu</param>
    /// <returns>Thông tin user + JWT token</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Đăng ký thành viên mới — POST /api/auth/register
    /// </summary>
    /// <param name="request">Thông tin đăng ký (email, password, tên, ...)</param>
    /// <returns>Thông tin user + JWT token</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        return CreatedAtAction(nameof(Register), response);
    }
}
