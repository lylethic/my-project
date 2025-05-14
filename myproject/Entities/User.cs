using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace myproject.Entities
{
  [Table("user")]
  public class User
  {
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign key
    public Guid RoleId { get; set; }

    // Navigation property
    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; } = null;
    public DateTime? DeletedAt { get; set; } = null;
    public bool IsActive { get; set; } = true;
  }
}
