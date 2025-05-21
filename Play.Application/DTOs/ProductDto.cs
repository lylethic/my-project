using System.ComponentModel.DataAnnotations;

namespace Play.Application.DTOs;

public record ProductDto(Guid Id, string Name, decimal Price, string? Description, DateTime CreatedAt, DateTime? UpdatedAt);
public record CreateProductDto([Required] string Name, [Required] decimal Price, string? Description, bool? IsActive);
public record UpdateProductDto(string ProductName, decimal? Price, string? Description, bool? IsActive);
