using System;
using System.ComponentModel.DataAnnotations;

namespace myproject.DTOs;

public record ProductDto(Guid Id, string Name, decimal Price, string? Description, DateTime CreatedAt, DateTime? UpdatedAt);
public record CreateProductDto([Required] string Name, [Required] decimal Price, string? Description);
public record UpdateProductDto(string Name, decimal? Price, string? Description);
