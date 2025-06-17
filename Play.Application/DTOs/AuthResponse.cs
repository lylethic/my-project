namespace Play.Application.DTOs
{
  public class AuthResponse
  {
    public int Status { get; set; }
    public string Message { get; set; } = "Login success";
    public string Token { get; set; } = string.Empty;     // JWT access token
    public DateTime TokenExpiredTime { get; set; } // Expiration datetime of the token
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiredTime { get; set; }

    public AuthResponse() { }
  }
}
