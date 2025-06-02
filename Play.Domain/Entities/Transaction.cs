using System;

namespace Play.Domain.Entities;

public class Transaction
{
  public string TransactionId { get; set; }
  public string? ProductId { get; set; }
  public string TransactionType { get; set; }
  public DateTime TransactionDate { get; set; } = DateTime.Now;
  public int Quantity { get; set; }
  public Product? Product { get; set; }
}
