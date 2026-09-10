namespace SportsCenterAPI.Helpers
{
    /// <summary>
    /// Helper class for securely hashing and verifying passwords using BCrypt.Net.
    /// Lớp tiện ích hỗ trợ băm và kiểm tra mật khẩu an toàn sử dụng thư viện BCrypt.Net.
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// Hashes a plain text password using BCrypt with automatic salting.
        /// Băm mật khẩu văn bản thô sử dụng thuật toán BCrypt kết hợp muối (salt) ngẫu nhiên.
        /// </summary>
        /// <param name="password">Plain text password / Mật khẩu văn bản thô</param>
        /// <returns>BCrypt hashed password string / Chuỗi mật khẩu đã được băm</returns>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));
            }

            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Verifies a plain text password against a stored BCrypt hash.
        /// Xác thực mật khẩu văn bản thô với chuỗi mật khẩu băm đã lưu trữ.
        /// </summary>
        /// <param name="password">Plain text password to verify / Mật khẩu văn bản thô cần kiểm tra</param>
        /// <param name="hash">Stored BCrypt hash / Chuỗi mật khẩu băm đã lưu trong CSDL</param>
        /// <returns>True if the password matches the hash; otherwise, false / True nếu mật khẩu trùng khớp, ngược lại False</returns>
        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                // Return false if hash format is invalid
                // Trả về false nếu định dạng chuỗi băm không hợp lệ
                return false;
            }
        }
    }
}
