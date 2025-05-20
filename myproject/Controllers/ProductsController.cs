using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myproject.DTOs;
using myproject.IRepository;

namespace myproject.Controllers;

[Route("api/v1/products")]
[ApiController]
[Authorize]
public class ProductsController : ControllerBase
{
  private readonly IProductService _productService;
  private readonly ILogger<ProductsController> _logger;

  public ProductsController(IProductService product, ILogger<ProductsController> logger)
  {
    this._productService = product;
    this._logger = logger;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll(QueryParameters parameters)
  {
    var stopwatch = Stopwatch.StartNew();
    _logger.LogInformation("GET /api/v1/products called with query: {@Parameters}", parameters);

    var product = await _productService.GetProductsAsync(parameters);

    stopwatch.Stop();
    _logger.LogInformation("GET /api/v1/products completed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);

    if (product.StatusCode != 200)
    {
      _logger.LogWarning("GET /api/v1/products returned {StatusCode}: {Message}", product.StatusCode, product.Message);
      return StatusCode(product.StatusCode, new
      {
        status = product.StatusCode,
        message = product.Message
      });
    }

    return Ok(new
    {
      status = product.StatusCode,
      message = product.Message,
      data = new
      {
        products = product.Data?.Items,
        pagination = new
        {
          totalItems = product.Data?.TotalItems,
          pageNumber = product.Data?.PageNumber,
          pageSize = product.Data?.PageSize,
          totalPages = product.Data?.TotalPages
        }
      }
    });
  }

  [HttpGet("{id}")]
  public async Task<IActionResult> GetProduct(Guid id)
  {
    var stopwatch = Stopwatch.StartNew();
    _logger.LogInformation("GET /api/v1/products/{@Id} called with query: {@Id}", id);

    // Validate input
    if (id == Guid.Empty)
    {
      return BadRequest(new { status = 400, message = "Invalid product ID" });
    }

    var product = await _productService.GetProductAsync(id);

    stopwatch.Stop();
    _logger.LogInformation("GET /api/v1/products/{@Id} completed in {ElapsedMilliseconds}ms", id, stopwatch.ElapsedMilliseconds);

    if (product.StatusCode != 200)
    {
      _logger.LogWarning("GET /api/v1/products/{@Id} returned {StatusCode}: {Message}", id, product.StatusCode, product.Message);
      return StatusCode(product.StatusCode, new
      {
        status = product.StatusCode,
        message = product.Message
      });
    }

    return StatusCode(product.StatusCode,
      new
      {
        status = product.StatusCode,
        message = product.Message,
        data = product.Data ?? new object()
      });
  }

  [HttpPost]
  [Authorize(Policy = "RequireOwnerAdminRole")]
  public async Task<IActionResult> CreateProduct(CreateProductDto entity)
  {
    if (entity is null) return BadRequest(new { message = "Please enter your product." });

    var product = await _productService.AddProductAsync(entity);
    if (product.StatusCode != 200) return StatusCode(product.StatusCode, new
    {
      status = product.StatusCode,
      message = product.Message
    });

    return StatusCode(product.StatusCode, new
    {
      status = product.StatusCode,
      data = product.Data
    });
  }

  [HttpPut("{id}")]
  [Authorize(Policy = "RequireOwnerAdminRole")]
  public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductDto entity)
  {
    var product = await _productService.UpdateProductAsync(id, entity);
    if (product.StatusCode == 404)
      return NotFound(product);

    return NoContent();
  }

  [HttpDelete("{id}")]
  [Authorize(Policy = "RequireOwnerAdminRole")]
  public async Task<IActionResult> DeleteProduct(Guid id)
  {
    var product = await _productService.DeleteProductAsync(id);
    if (product is null) return NotFound(product);

    return NoContent();
  }

  [Authorize(Policy = "RequireOwnerAdminRole")]
  [HttpPost("import")]
  public async Task<IActionResult> ImportUsers(IFormFile file)
  {

    if (file == null || file.Length == 0)
      return BadRequest("No file uploaded.");

    var result = await _productService.ImportProductsAsync(file);

    if (result.StatusCode == 200)
      return Ok(new
      {
        status = result.StatusCode,
        message = result.Message
      });

    return StatusCode(result.StatusCode, new
    {
      status = result.StatusCode,
      message = result.Message
    });
  }

}
