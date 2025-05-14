namespace myproject.DTOs
{
  public class AuthResponse
  {
    public int Status { get; set; }
    public string Message { get; set; } = "Login success";
    public string Token { get; set; } = string.Empty;     // JWT access token
    public DateTime TokenExpiredTime { get; set; } // Expiration datetime of the token
  }
}
