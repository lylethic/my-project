using System.ComponentModel.DataAnnotations;

namespace Play.Application.DTOs;

public class UserDto
{
  public string Id { get; set; } = Guid.NewGuid().ToString();
  public string RoleId { get; set; }
  public string FirstName { get; set; }
  public string LastName { get; set; }
  public string Email { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public bool IsActive { get; set; } = true;
}

public record UpdateUserRequest(string RoleId, string FirstName, string LastName, string Email, bool IsActive = true);

public record CreateUserRequest(
    [Required(ErrorMessage = "RoleId is required")]
    string RoleId,

    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2–50 characters")]
    string FirstName,

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2–50 characters")]
    string LastName,

    [Required(ErrorMessage = "Email is required")]
    [MaxLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    string Email,

    [Required(ErrorMessage = "Password is required")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Password must contain uppercase, lowercase, digit, special character and be 8+ characters long")]
    string Password
);

