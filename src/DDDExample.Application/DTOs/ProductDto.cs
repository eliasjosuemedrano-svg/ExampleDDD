namespace DDDExample.Application.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public record CreateProductDto(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string CategoryId);

public record UpdateProductDto(
    string Name,
    string Description,
    decimal Price,
    string CategoryId);

public record UpdateProductStockDto(int Quantity);
