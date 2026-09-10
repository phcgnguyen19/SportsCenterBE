using SportsCenterAPI.DTOs.Auth;

namespace SportsCenterAPI.Services.Interface
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
    }
}
