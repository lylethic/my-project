using System.ComponentModel.DataAnnotations;

namespace myproject.DTOs;

public record UserDto(Guid Id, Guid RoleId, string Name, string Email);
// public record CreateUserDto([Required] Guid RoleId, [Required] string Name, [Required][EmailAddress(ErrorMessage = "Invalid email format")] string Email, [Required] string Password);
public record UpdateUserDto(Guid RoleId, string Name, string Email, bool IsActive = true);

public record CreateUserDto(
    Guid RoleId,

    [Required(ErrorMessage = "Name is required")]
    string Name,

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    string Email,

    [Required(ErrorMessage = "Password is required")]
    string Password
) : IValidatableObject
{
  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (RoleId == Guid.Empty)
    {
      yield return new ValidationResult("RoleId cannot be empty", new[] { nameof(RoleId) });
    }
  }
}
