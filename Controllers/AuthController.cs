using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsCenterAPI.Models.DTOs.Auth;
using SportsCenterAPI.Models.DTOs.Login;
using SportsCenterAPI.Models.DTOs.Register;
using SportsCenterAPI.Services.Interface;

namespace SportsCenterAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDTO request)
    {
        var response = await authService.LoginAsync(request);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("send-register-otp")]
    public async Task<IActionResult> SendRegisterOtp(
        [FromBody] RegisterRequestDTO request)
    {
        await authService.SendRegisterOtpAsync(request);
        return Ok(new { message = "Registration OTP sent." });
    }

    [AllowAnonymous]
    [HttpPost("verify-register-otp")]
    public async Task<IActionResult> VerifyRegisterOtp(
        [FromBody] VerifyRegisterOtpDTO request)
    {
        var response = await authService.VerifyRegisterOtpAsync(request);
        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("request-reset-password")]
    public async Task<IActionResult> RequestResetPasswordOtp(
        [FromBody] RequestOtpDTO request)
    {
        await authService.RequestResetPasswordOtpAsync(request);

        return Ok(new
        {
            message = "If the account exists, an OTP has been sent."
        });
    }

    [AllowAnonymous]
    [HttpPost("verify-reset-password")]
    public async Task<IActionResult> VerifyResetPasswordOtp(
        [FromBody] VerifyResetPasswordOtpDTO request)
    {
        await authService.VerifyResetPasswordOtpAsync(request);
        return Ok(new { message = "Password updated successfully." });
    }
}