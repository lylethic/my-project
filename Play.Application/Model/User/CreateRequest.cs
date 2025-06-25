using System;
using System.ComponentModel.DataAnnotations;

namespace Play.Application.Model.User;

public class CreateRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string RoleId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string? FirstName { get; set; }

    [Required]
    public string? LastName { get; set; }

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [MinLength(6)]
    public string? Password { get; set; }

    [Required]
    [Compare("Password")]
    public string? ConfirmPassword { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; } = null;
    public DateTime? DeletedAt { get; set; } = null;
    public bool IsActive { get; set; } = true;
}
