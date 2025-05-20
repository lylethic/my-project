using System.ComponentModel.DataAnnotations;

namespace Play.Application.DTOs;

public record UserDto(Guid Id, Guid RoleId, string Name, string Email);
// public record CreateUserDto([Required] Guid RoleId, [Required] string Name, [Required][EmailAddress(ErrorMessage = "Invalid email format")] string Email, [Required] string Password);
public record UpdateUserDto(Guid RoleId, string Name, string Email, bool IsActive = true);

public record CreateUserDto(
    Guid RoleId,

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2-100 characters")]
    [RegularExpression(@"^[\p{L}\s'-]+$", ErrorMessage = "Name contains invalid characters")]
    string Name,

    [Required(ErrorMessage = "Email is required")]
    [MaxLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
    ErrorMessage = "Invalid email format")]
    string Email,

    [Required(ErrorMessage = "Password is required")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "Password must be at least 8 characters with uppercase, lowercase, number, and special character")]
    string Password
) : IValidatableObject
{
  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (RoleId == Guid.Empty)
    {
      yield return new ValidationResult("RoleId cannot be empty", [nameof(RoleId)]);
    }

    // Example: Additional business rule validation
    if (Name?.Trim().Length < 2)
    {
      yield return new ValidationResult("Name is too short", [nameof(Name)]);
    }
  }
}
