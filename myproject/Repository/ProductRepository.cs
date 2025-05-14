using Microsoft.EntityFrameworkCore;
using myproject.Data;
using myproject.DTOs;
using myproject.Entities;
using myproject.IRepository;

namespace myproject.Repository
{
  public class ProductRepository : IProductService
  {
    private readonly ApiDbContext _context;
    public ProductRepository(ApiDbContext context)
    {
      this._context = context;
    }

    public async Task<ResponseData<Product>> AddProductAsync(CreateProductDto entity)
    {
      var nameExisting = await _context.Products
        .Where(x => x.Name.ToLower() == entity.Name.ToLower())
        .FirstOrDefaultAsync();

      if (nameExisting is not null)
      {
        return ResponseData<Product>.Fail("Product name already exists.", 400);
      }

      var product = new Product
      {
        Name = entity.Name,
        Price = entity.Price,
        Description = entity.Description,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = null
      };

      await _context.Products.AddAsync(product);
      await _context.SaveChangesAsync();

      return ResponseData<Product>.Success(product);
    }


    public async Task<ResponseData<Product>> GetProductAsync(Guid id)
    {
      var find = await _context.Products.Where(x => x.Id == id).FirstOrDefaultAsync();
      if (find is null) return ResponseData<Product>.Fail("Not found", 404);
      return ResponseData<Product>.Success(find);
    }

    public async Task<ResponseData<Product>> GetProductsAsync()
    {
      var products = await _context.Products.ToListAsync();
      if (!products.Any())
        return ResponseData<Product>.Fail("No products found", 404);

      return ResponseData<Product>.Success(products);
    }

    public async Task<ResponseData<Product>> UpdateProductAsync(Guid id, UpdateProductDto entity)
    {
      var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == id);
      if (product is null) return ResponseData<Product>.Fail("Product not found", 404);

      product.Name = entity.Name ?? product.Name;
      product.Price = entity.Price ?? 0;
      product.Description = entity.Description ?? product.Description;
      product.UpdatedAt = DateTime.UtcNow;

      _context.Products.Update(product);
      _context.SaveChanges();

      return ResponseData<Product>.Success(product);
    }

    public async Task<ResponseData<Product>> DeleteProductAsync(Guid id)
    {
      var productExisting = await _context.Products.Where(x => x.Id == id).FirstOrDefaultAsync();
      if (productExisting is null)
      {
        return ResponseData<Product>.Fail("Product Not found", 404);
      }

      _context.Products.Remove(productExisting);
      await _context.SaveChangesAsync();
      return ResponseData<Product>.Success(productExisting);
    }
  }
}
