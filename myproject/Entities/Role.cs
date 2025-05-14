using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace myproject.Entities;

[Table("role")]
public class Role
{
  [Key]
  public Guid Id { get; set; }
  public required string Name { get; set; }
  public required string Description { get; set; }
}
