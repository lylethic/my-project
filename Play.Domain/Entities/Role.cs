using Dapper.Contrib.Extensions;

namespace Play.Domain.Entities;

[Table("roles")]
public class Role
{
  [Key]
  public string Id { get; set; } = Guid.NewGuid().ToString();
  public string Name { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public DateTime? DeletedAt { get; set; }
  public bool IsActive { get; set; } = true;
}
