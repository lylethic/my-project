using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using myproject.Data;
using myproject.DTOs;
using myproject.Entities;
using myproject.Helpers;
using myproject.IRepository;
using Npgsql;

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
      try
      {
        var nameExisting = await _context.Products
          .Where(x => x.ProductName
          .ToLower() == entity.Name.ToLower())
          .FirstOrDefaultAsync();

        if (nameExisting is not null)
        {
          return ResponseData<Product>.Fail("Product name already exists.", 400);
        }

        var product = new Product
        {
          ProductName = entity.Name,
          Price = entity.Price,
          Description = entity.Description,
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = null
        };

        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
        return ResponseData<Product>.Success(product);
      }
      catch (Exception ex)
      {
        return ResponseData<Product>.Fail("Error creating product", 500);
        throw new AppException($"An error occurred while creating product: {ex.Message}");
      }
    }

    public async Task<ResponseData<Product>> GetProductAsync(Guid id)
    {
      try
      {
        var find = await _context.Products
        .AsNoTracking()
        .Where(x => x.Id == id)
        .FirstOrDefaultAsync();

        if (find is null) return ResponseData<Product>.Fail("Not found", 404);
        return ResponseData<Product>.Success(find);
      }
      catch (NpgsqlException ex) // Be specific with database exceptions if possible
      {
        return ResponseData<Product>.Fail("Database error", 500);
        throw new AppException($"Database error while fetching product {id}. {ex.Message}", 500);
      }
      catch (Exception ex)
      {
        return ResponseData<Product>.Fail("An error occurred", 500);
        throw new AppException($"An error occurred while retrieving products: {ex.Message}");
      }
    }

    public async Task<ResponseData<PaginatedResponse<Product>>> GetProductsAsync(QueryParameters parameters)
    {
      try
      {
        var query = _context.Products.AsQueryable();
        // Apply search if SearchTerm is provided
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
          var searchTerm = parameters.SearchTerm.ToLower();
          query = query.Where(u =>
              u.ProductName.ToLower().Contains(searchTerm)
          );
        }

        // Calculate total
        int totalRecords = await query.CountAsync();

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(parameters.SortBy))
        {
          query = parameters.SortBy.ToLower() switch
          {
            "name" => parameters.SortDescending
                       ? query.OrderByDescending(u => u.ProductName)
                       : query.OrderBy(u => u.ProductName),
            "price" => parameters.SortDescending
                        ? query.OrderByDescending(u => u.Price)
                        : query.OrderBy(u => u.Price),
            "createdat" => parameters.SortDescending
                            ? query.OrderByDescending(u => u.CreatedAt)
                            : query.OrderBy(u => u.CreatedAt),
            _ => parameters.SortDescending
                 ? query.OrderByDescending(u => u.Id)
                 : query.OrderBy(u => u.Id) // Default sort by Id
          };
        }
        else
        {
          // Default sorting by Id if no sort field specified
          query = query.OrderBy(u => u.Id);
        }

        // Apply pagination
        var products = await query
        .AsNoTracking()
        .Skip(parameters.Page - 1 * parameters.PageSize)
        .Take(parameters.PageSize)
        .Select(p => new Product
        {
          Id = p.Id,
          ProductName = p.ProductName,
          Price = p.Price,
          Description = p.Description,
          CreatedAt = p.CreatedAt
        })
        .ToListAsync();

        var paginatedResponse = new PaginatedResponse<Product>
        {
          Items = products,
          TotalItems = totalRecords,
          PageNumber = parameters.Page,
          PageSize = parameters.PageSize
        };

        if (products.Count == 0)
          return ResponseData<PaginatedResponse<Product>>.Fail("No products found.", 404);

        return ResponseData<PaginatedResponse<Product>>.Success(paginatedResponse);
      }
      catch (DbUpdateException dbEx)
      {
        throw new AppException($"Database error occurred while retrieving products: {dbEx.Message}");
      }
      catch (InvalidOperationException ioEx)
      {
        throw new AppException($"Invalid operation while retrieving products: {ioEx.Message}");
      }
      catch (Exception ex)
      {
        return ResponseData<PaginatedResponse<Product>>.Fail("Error retrieving products", 500);
        throw new AppException($"An error occurred while retrieving products: {ex.Message}");
      }
    }

    public async Task<ResponseData<Product>> UpdateProductAsync(Guid id, UpdateProductDto entity)
    {
      var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == id);
      if (product is null) return ResponseData<Product>.Fail("Product not found", 404);

      product.ProductName = entity.Name ?? product.ProductName;
      product.Price = entity.Price ?? 0;
      product.Description = entity.Description ?? product.Description;
      product.UpdatedAt = DateTime.UtcNow;

      _context.Products.UpdateRange(product);
      await _context.SaveChangesAsync();

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

    public async Task<ResponseData<string>> ImportProductsAsync(IFormFile file)
    {
      await using var transaction = await _context.Database.BeginTransactionAsync();

      try
      {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1); // Sheet đầu tiên
        var rows = worksheet.RowsUsed().Skip(1); // Bỏ qua dòng tiêu đề

        foreach (var row in rows)
        {
          var name = row.Cell(1).GetString()?.Trim();
          var priceStr = row.Cell(2).GetString();
          var description = row.Cell(3).GetString()?.Trim();

          if (string.IsNullOrWhiteSpace(name)) continue;

          if (!decimal.TryParse(priceStr, out var price))
            return ResponseData<string>.Fail($"Invalid price at product: {name}", 400);

          // Kiểm tra trùng tên sản phẩm
          var existing = await _context.Products
              .FirstOrDefaultAsync(p => p.ProductName.ToLower() == name.ToLower());

          if (existing != null)
            return ResponseData<string>.Fail($"Product '{name}' already exists.", 400);

          var product = new Product
          {
            ProductName = name,
            Price = price,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
          };

          await _context.Products.AddAsync(product);
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return ResponseData<string>.Success("Import thành công.");
      }
      catch (Exception ex)
      {
        await transaction.RollbackAsync();
        return ResponseData<string>.Fail("Lỗi khi import: " + ex.Message, 500);
      }
    }
  }
}
