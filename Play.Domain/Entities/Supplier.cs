using System;

namespace Play.Domain.Entities;

public class Supplier
{
  public string SupplierId { get; set; }
  public string SupplierName { get; set; }
  public string? ContactEmail { get; set; }
  public string? PhoneNumber { get; set; }
  public List<Inventory> Inventories { get; set; } = new List<Inventory>();
}
