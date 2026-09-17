using SportsCenterAPI.Models.DTOs.Auth;
using SportsCenterAPI.Models.DTOs.Login;
using SportsCenterAPI.Models.DTOs.Register;

namespace SportsCenterAPI.Services.Interface
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequestDTO request);
        Task<AuthResponse> RegisterAsync(RegisterRequestDTO request);

        Task<bool> IsEmailExistingAsync(string email);
    }
}
