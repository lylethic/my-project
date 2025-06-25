using System;
using System.ComponentModel.DataAnnotations;

namespace Play.Application.Model.User;

public class UpdateRequest
{
    public string? Id { get; set; }
    public string? RoleId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    private string? _password;
    [MinLength(6)]
    public string? Password
    {
        get => _password;
        set => _password = replaceEmptyWithNull(value);
    }

    [Required]
    [Compare("Password")]
    public string? ConfirmPassword { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; } = null;
    public DateTime? DeletedAt { get; set; } = null;
    public bool IsActive { get; set; } = true;

    private string? replaceEmptyWithNull(string? value)
    {
        // replace empty string with null to make field optional
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
