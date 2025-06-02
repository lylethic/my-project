using System;

namespace Play.Domain.Entities;

public class Category
{
  public string CategoryId { get; set; }
  public string CategoryName { get; set; }
  public string? Description { get; set; }
  public List<Product> Products { get; set; } = new List<Product>();
}
