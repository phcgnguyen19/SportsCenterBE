using SportsCenterAPI.Models.DTOs.Accounts;
using SportsCenterAPI.Models.DTOs.Auth;
using SportsCenterAPI.Models.DTOs.Login;
using SportsCenterAPI.Models.DTOs.Register;

namespace SportsCenterAPI.Services.Interface;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequestDTO request);

    // Giữ cho luồng đăng ký trực tiếp nếu nơi khác đang sử dụng.
    // Không gọi hàm này từ endpoint đăng ký công khai khi bắt buộc OTP.
    Task<AuthResponse> RegisterAsync(RegisterRequestDTO request);

    Task<MemberResponseDTO> RegisterAtDeskAsync(RegisterRequestDTO request);
    Task<bool> IsEmailExistingAsync(string email);

    Task SendRegisterOtpAsync(RegisterRequestDTO request);
    Task<AuthResponse> VerifyRegisterOtpAsync(VerifyRegisterOtpDTO request);

    Task RequestResetPasswordOtpAsync(RequestOtpDTO request);
    Task VerifyResetPasswordOtpAsync(VerifyResetPasswordOtpDTO request);
}