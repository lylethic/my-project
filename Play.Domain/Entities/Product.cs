using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Play.Domain.Entities;

[Table("product")]
public class Product
{
  [Key]
  public Guid Id { get; set; }
  public required string ProductName { get; set; }
  public decimal Price { get; set; }
  public string? Description { get; set; }
  public DateTime? CreatedAt { get; set; } = DateTime.Now;
  public DateTime? UpdatedAt { get; set; }
  public bool IsActive { get; set; } = true;
}