using System.ComponentModel.DataAnnotations;

namespace myproject.DTOs;

public record UserDto(Guid Id, Guid RoleId, string Name, string Email);
public record CreateUserDto([Required] Guid RoleId, [Required] string Name, [Required] string Email, [Required] string Password);
public record UpdateUserDto(Guid RoleId, string Name, string Email);
