using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SportsCenterAPI.Models.DTOs.API;
using SportsCenterAPI.Models.DTOs.Auth;
using SportsCenterAPI.Models.DTOs.Login;
using SportsCenterAPI.Models.DTOs.Register;
using SportsCenterAPI.Models.DTOs.User;
using SportsCenterAPI.Services.Implement;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Controllers;

/// <summary>
/// Controller xử lý xác thực: đăng nhập, đăng ký
/// Authentication controller: login and registration endpoints
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IMapper _mapper;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Đăng nhập — POST /api/auth/login
    /// </summary>
    /// <param name="request">Email và mật khẩu</param>
    /// <returns>Thông tin user + JWT token</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<LoginResponseDTO>>> Login([FromBody] LoginRequestDTO loginRequestDTO)
    {
        if (loginRequestDTO == null)
        {
            return BadRequest(ApiResponse<object>.BadRequest("Invalid login request", new { Error = "Request body is null" }));
        }

        var loginResponse = await _authService.LoginAsync(loginRequestDTO);

        if (loginResponse == null)
        {
            return BadRequest(ApiResponse<object>.BadRequest("Login failed"));
        }

        //auth service will return a response with user info and JWT token
        var response = ApiResponse<AuthResponse>.Ok(loginResponse, "Login successful");
        return Ok(response);

        var errorResponse = ApiResponse<object>.Error(StatusCodes.Status500InternalServerError, "An error occurred during login");

        return StatusCode(500, errorResponse);
    }

    /// <summary>
    /// Đăng ký thành viên mới — POST /api/auth/register
    /// </summary>
    /// <param name="request">Thông tin đăng ký (email, password, tên, ...)</param>
    /// <returns>Thông tin user + JWT token</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<UserDTO>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
    {
        if(request == null)
        {
            return BadRequest(ApiResponse<object>.BadRequest("Registration data is required"));
        }
        
        if(await _authService.IsEmailExistingAsync(request.Email))
        {
            return Conflict(ApiResponse<object>.Conflict($"User with email {request.Email} already exists"));
        }

        var user = await _authService.RegisterAsync(request);

        if (user == null)
        {
            return BadRequest(ApiResponse<object>.BadRequest("Registration failed"));
        }



        var response = ApiResponse<AuthResponse>.Ok(user, "Registration successful");
        return CreatedAtAction(nameof(Register), response);
    }
}
