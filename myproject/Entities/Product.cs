using System;
using System.ComponentModel.DataAnnotations;

namespace myproject.Entities;

public class Product
{
  [Key]
  public Guid Id { get; set; }
  public required string ProductName { get; set; }
  public decimal Price { get; set; }
  public string? Description { get; set; }
  public DateTime? CreatedAt { get; set; } = DateTime.Now;
  public DateTime? UpdatedAt { get; set; }
}