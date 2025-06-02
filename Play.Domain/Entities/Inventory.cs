using System;

namespace Play.Domain.Entities;

public class Inventory
{
  public string InventoryId { get; set; }
  public string? ProductId { get; set; }
  public int Quantity { get; set; } = 0;
  public string? SupplierId { get; set; }
  public DateTime LastUpdated { get; set; } = DateTime.Now;
  public Product? Product { get; set; }
  public Supplier? Supplier { get; set; }
}
