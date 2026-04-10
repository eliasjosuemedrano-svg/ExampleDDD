using DDDExample.Domain.Common;

namespace DDDExample.Domain.Entities;

public class Product : Entity<Guid>, IAggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public string CategoryId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Product() { } // For EF Core and MongoDB

    public Product(Guid id, string name, string description, decimal price, int stock, string categoryId)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ArgumentException("Category ID is required", nameof(categoryId));
            
        Id = id;
        Name = name;
        Description = description;
        Price = price > 0 ? price : throw new ArgumentException("Price must be greater than zero.");
        Stock = stock >= 0 ? stock : throw new ArgumentException("Stock cannot be negative.");
        CategoryId = categoryId;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string description, decimal price, string categoryId)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ArgumentException("Category ID is required", nameof(categoryId));
            
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Price = price > 0 ? price : throw new ArgumentException("Price must be greater than zero.");
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStock(int quantity)
    {
        if (Stock + quantity < 0)
            throw new InvalidOperationException("Insufficient stock.");
            
        Stock += quantity;
        UpdatedAt = DateTime.UtcNow;
    }
}
