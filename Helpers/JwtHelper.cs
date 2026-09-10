using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SportsCenterAPI.Models;

namespace SportsCenterAPI.Helpers
{
    /// <summary>
    /// Helper class for generating JSON Web Tokens (JWT) for authentication.
    /// Lớp tiện ích hỗ trợ tạo JWT token dùng cho việc xác thực và phân quyền người dùng.
    /// </summary>
    public static class JwtHelper
    {
        /// <summary>
        /// Generates a signed JWT token containing user claims.
        /// Tạo chuỗi JWT token đã ký chứa các thông tin (claims) của người dùng.
        /// </summary>
        /// <param name="user">User entity / Đối tượng người dùng</param>
        /// <param name="secretKey">Symmetric secret key used for signing / Khóa bí mật dùng để ký token</param>
        /// <param name="issuer">Issuer identifier / Định danh đơn vị cấp phát token</param>
        /// <param name="audience">Audience identifier / Định danh đối tượng tiếp nhận token</param>
        /// <param name="expireMinutes">Token lifetime in minutes (default is 60 minutes) / Thời hạn token tính bằng phút</param>
        /// <returns>Signed JWT token string / Chuỗi JWT token đã được ký</returns>
        public static string GenerateToken(
            User user,
            string secretKey,
            string issuer,
            string audience,
            int expireMinutes = 60)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "User cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new ArgumentException("Secret key cannot be null or empty.", nameof(secretKey));
            }

            // Prepare key and signing credentials (HmacSha256)
            // Chuẩn bị khóa và thuật toán ký mã hóa HmacSha256
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Populate required claims: UserId, Email, FullName, Role
            // Thêm các claim theo yêu cầu hệ thống và các claim tiêu chuẩn
            var claims = new List<Claim>
            {
                new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("Email", user.Email ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("FullName", user.FullName ?? string.Empty),
                new Claim(ClaimTypes.Name, user.FullName ?? string.Empty),
                new Claim("Role", user.Role ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Configure token descriptor
            // Cấu hình thông tin phát hành token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
