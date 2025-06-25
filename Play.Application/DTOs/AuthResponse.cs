using Play.Application.Enums;

namespace Play.Application.DTOs
{
    public class AuthResponse
    {
        public int Status { get; set; }
        public string Message { get; set; } = "Login success";
        public string Token { get; set; } = string.Empty;     // JWT access token
        public DateTime TokenExpiredTime { get; set; } // Expiration datetime of the token

        public AuthResponse() { }

        public AuthResponse(AuthStatus status, string message)
        {
            Status = (int)status;
            Message = message;
        }

        public AuthResponse(AuthStatus status, string message, string token, DateTime tokenExpireTime)
        {
            Status = (int)status;
            Message = message;
            Token = token;
            TokenExpiredTime = tokenExpireTime;
        }
    }
}
