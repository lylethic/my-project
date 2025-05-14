using System;

namespace myproject.Entities;

public class Product
{
  public Guid Id { get; set; }
  public required string Name { get; set; }
  public decimal Price { get; set; }
  public string? Description { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.Now;
  public DateTime? UpdatedAt { get; set; }
}