using System.ComponentModel.DataAnnotations;

namespace Play.Application.DTOs;

/// <summary> "record" keyword: compare based on values of their properties, not object references.
/// </summary>
public class RoleDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
public record CreateRoleDto(string Name, bool IsActive);
public record CreateRoleRequest(string Id, string Name, bool IsActive);
public record UpdateRoleRequest(string Id, string Name, bool IsActive, DateTime? DeletedAt = null);