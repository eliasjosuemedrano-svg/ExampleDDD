using DDDExample.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DDDExample.Infrastructure.Persistence.SqlServer;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(1000);
                
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)");
                
            entity.Property(e => e.CreatedAt)
                .IsRequired();
                
            entity.HasIndex(e => e.Name);
        });
    }
}
