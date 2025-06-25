using System.ComponentModel.DataAnnotations;

namespace Play.Application.DTOs;

public class UserDto
{
  public string Id { get; set; } = Guid.NewGuid().ToString();
  public string RoleId { get; set; }
  public string FirstName { get; set; }
  public string LastName { get; set; }
  public string Email { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.Now;
  public bool IsActive { get; set; } = true;
}

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

public record UpdateUserRequest
{
  public string Id { get; init; } = string.Empty;
  public string? RoleId { get; init; }

  [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "FirstName must contain only letters, spaces, hyphens, or apostrophes.")]
  [StringLength(100, ErrorMessage = "FirstName cannot exceed 100 characters.")]
  public string? FirstName { get; init; }

  [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "LastName must contain only letters, spaces, hyphens, or apostrophes.")]
  [StringLength(100, ErrorMessage = "LastName cannot exceed 100 characters.")]
  public string? LastName { get; init; }

  [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
  [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
  public string? Email { get; init; }

  public bool? IsActive { get; init; }
}


