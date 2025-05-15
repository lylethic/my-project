using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myproject.DTOs;
using myproject.IRepository;

namespace myproject.Controllers;

[Route("api/v1/products")]
[ApiController]
public class ProductsController : ControllerBase
{
  private readonly IProductService _productService;

  public ProductsController(IProductService product)
  {
    this._productService = product;
  }

  [HttpGet("test-conn")]
  public IActionResult All()
  {
    return Ok(new
    {
      message = "Connection!"
    });
  }

  [HttpGet]
  public async Task<IActionResult> GetAll()
  {
    var product = await _productService.GetProductsAsync();
    if (product.StatusCode != 200)
      return StatusCode(
        product.StatusCode,
        new
        {
          status = product.StatusCode,
          message = product.Message
        });

    return Ok(new
    {
      message = product.Message,
      data = product.ListData
    });
  }

  [HttpGet("{id}")]
  public async Task<IActionResult> GetProduct(Guid id)
  {
    var product = await _productService.GetProductAsync(id);
    if (product.StatusCode != 200)
      return NotFound(product);

    return StatusCode(product.StatusCode,
      new
      {
        message = product.Message,
        data = product.Data
      });
  }

  [HttpPost]
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
  public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductDto entity)
  {
    var product = await _productService.UpdateProductAsync(id, entity);
    if (product.StatusCode == 404)
      return NotFound(product);

    return NoContent();
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> DeleteProduct(Guid id)
  {
    var product = await _productService.DeleteProductAsync(id);
    if (product is null) return NotFound(product);

    return NoContent();
  }
}
