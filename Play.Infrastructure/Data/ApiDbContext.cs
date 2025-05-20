using Microsoft.EntityFrameworkCore;
using Play.Domain.Entities;

namespace Play.Infrastructure.Data;

public class ApiDbContext : DbContext
{
  public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
  {

  }

  public DbSet<User> Users { get; set; }
  public DbSet<Role> Roles { get; set; }
  public DbSet<Product> Products { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    // modelBuilder.Ignore<Product>(); // This ensures Product is excluded

    modelBuilder.Entity<User>()
               .HasIndex(u => u.Name)
               .HasDatabaseName("IX_User_Name");

    modelBuilder.Entity<User>()
        .HasIndex(u => u.Email)
        .IsUnique()
        .HasDatabaseName("IX_User_Email");
  }
}
