using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Play.Domain.Entities;

[Table("product")]
public class Product
{
  public string ProductId { get; set; }
  public string ProductName { get; set; }
  public decimal Price { get; set; }
  public string? CategoryId { get; set; }
  public Category? Category { get; set; }
  public List<Inventory> Inventories { get; set; } = new List<Inventory>();
  public List<Transaction> Transactions { get; set; } = new List<Transaction>();
}