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
}
