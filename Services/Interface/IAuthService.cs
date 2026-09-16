using SportsCenterAPI.DTOs.Request;
using SportsCenterAPI.DTOs.Response;

namespace SportsCenterAPI.Services.Interface
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequestDTO request);
        Task<AuthResponse> RegisterAsync(RegisterRequestDTO request);

        Task<bool> IsEmailExistingAsync(string email);
    }
}
