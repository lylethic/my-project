using System.ComponentModel.DataAnnotations;

namespace Play.Application.DTOs;

/// <summary> "record" keyword: compare based on values of their properties, not object references.
/// </summary>
public record RoleDto(Guid Id, string Name, string Description);
public record CreateRoleDto([Required] string Name, string Description);
public record UpdateRoleDto(string Name, string Description);