using myproject.DTOs;
using myproject.Entities;

namespace myproject.IRepository
{
  public interface IProductService
  {
    Task<ResponseData<Product>> GetProductsAsync();
    Task<ResponseData<Product>> GetProductAsync(Guid id);
    Task<ResponseData<Product>> AddProductAsync(CreateProductDto entity);
    Task<ResponseData<Product>> UpdateProductAsync(Guid id, UpdateProductDto entity);
    Task<ResponseData<Product>> DeleteProductAsync(Guid id);
  }
}
