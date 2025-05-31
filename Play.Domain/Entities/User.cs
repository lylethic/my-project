using Dapper.Contrib.Extensions;

namespace Play.Domain.Entities
{
  [Table("users")]
  public class User
  {
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RoleId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = null;
    public DateTime? DeletedAt { get; set; } = null;
    public bool IsActive { get; set; } = true;
  }
}
