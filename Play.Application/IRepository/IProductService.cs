using Microsoft.AspNetCore.Http;
using Play.Application.DTOs;
using Play.Domain.Entities;

namespace Play.Application.IRepository
{
  public interface IProductService
  {
    Task<ResponseData<PaginatedResponse<Product>>> GetProductsAsync(QueryParameters parameters);
    Task<ResponseData<Product>> GetProductAsync(Guid id);
    Task<ResponseData<Product>> AddProductAsync(CreateProductDto entity);
    Task<ResponseData<Product>> UpdateProductAsync(Guid id, UpdateProductDto entity);
    Task<ResponseData<Product>> DeleteProductAsync(Guid id);
    Task<ResponseData<string>> ImportProductsAsync(IFormFile file);
    Task<ResponseData<Product>> ChangeStatusProductAsync(Guid id);
  }
}
