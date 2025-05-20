namespace Play.Application.DTOs
{
  public class AuthenticateDto
  {
    public required string Email { get; set; }
    public required string Password { get; set; }
  }
}
