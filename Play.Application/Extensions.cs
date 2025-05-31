using Play.Application.DTOs;
using Play.Domain.Entities;


namespace Play.Application
{
  // This class use for: Mapping data
  public static class Extentions
  {
    public static ProductDto AsDto(this Product item)
    {
      return new ProductDto(item.Id, item.ProductName, item.Price, item.Description, item.CreatedAt ?? DateTime.Now, item.UpdatedAt);
    }
  }
}