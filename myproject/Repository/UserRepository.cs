using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using myproject.Data;
using myproject.DTOs;
using myproject.Entities;
using myproject.IRepository;
using Npgsql;

namespace myproject.Repository
{
  public class UserRepository : IUserService
  {
    private readonly ApiDbContext _context;

    public UserRepository(ApiDbContext context)
    {
      this._context = context;
    }

    // PasswordHasher: base64, salt, hashed password
    private readonly PasswordHasher<User> _passwordHasher = new();

    // Separate method for hashing password
    private string HashPassword(User user, string plainPassword)
    {
      return _passwordHasher.HashPassword(user, plainPassword);
    }

    public async Task<ResponseData<UserDto>> GetUsersAsync(bool? isActive = true)
    {
      try
      {
        var users = await _context.Users
        .Where(u => u.IsActive == isActive)
        .Select(u => new UserDto(u.Id, u.RoleId, u.Name, u.Email))
        .ToListAsync();

        if (users.Count == 0)
          return ResponseData<UserDto>.Fail("No users found.", 404);

        return ResponseData<UserDto>.Success(users);
      }
      catch (NpgsqlException ex)
      {
        // Log ex.Message if needed
        return ResponseData<UserDto>.Fail("Database connection failed.", 500);
        throw new Exception(ex.Message);
      }
      catch (Exception ex)
      {
        return ResponseData<UserDto>.Fail("Server error", 500);
        throw new Exception(ex.Message);
      }
    }

    public async Task<ResponseData<UserDto>> GetUserAsync(Guid id)
    {
      try
      {
        var user = await _context.Users
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            (
               u.Id,
               u.RoleId,
               u.Name ?? string.Empty, // Handle null Name
               u.Email ?? string.Empty // Handle null Email
            ))
            .FirstOrDefaultAsync();

        if (user == null)
          return ResponseData<UserDto>.Fail("User not found", 404);

        return ResponseData<UserDto>.Success(user);
      }
      catch (NpgsqlException ex)
      {
        // Log database errors properly
        Console.WriteLine($"Database error: {ex.Message}");
        return ResponseData<UserDto>.Fail("Database error", 500);
      }
      catch (Exception ex)
      {
        // Log general errors
        Console.WriteLine($"Error: {ex}");
        return ResponseData<UserDto>.Fail("An error occurred", 500);
      }
    }

    public async Task<ResponseData<User>> AddUserAsync(CreateUserDto entity)
    {
      try
      {
        // Custom Email Format Check
        if (!Helpers.Utils.IsValidEmail(entity.Email))
        {
          return ResponseData<User>.Fail("Invalid email format!", 400);
        }

        var checkRole = await _context.Roles.Where(x => x.Id == entity.RoleId).FirstOrDefaultAsync();
        if (checkRole is null) return ResponseData<User>.Fail("Role not found", 404);

        var checkEmailExisting = await _context.Users.Where(x => x.Email == entity.Email).FirstOrDefaultAsync();
        if (checkEmailExisting is not null) return ResponseData<User>.Fail("Email existed.", 400);

        var user = new User
        {
          Id = Guid.NewGuid(),
          RoleId = entity.RoleId,
          Name = entity.Name,
          Email = entity.Email,
          Password = "temp",
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = null,
          DeletedAt = null
        };
        user.Password = HashPassword(user, entity.Password);

        await _context.AddAsync(user);
        await _context.SaveChangesAsync();
        return ResponseData<User>.Success(user);
      }
      catch (Exception ex)
      {
        return ResponseData<User>.Fail("Server error.", 500);
        throw new Exception(ex.Message);
      }
    }

    public async Task<ResponseData<User>> UpdateUserAsync(Guid id, UpdateUserDto entity)
    {
      try
      {
        var checkRole = await _context.Roles.Where(x => x.Id == entity.RoleId).FirstOrDefaultAsync();
        if (checkRole is null) return ResponseData<User>.Fail("Role not found");

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null) return ResponseData<User>.Fail("User not found!", 404);

        user.RoleId = entity.RoleId;
        user.Name = entity.Name ?? user.Name;
        user.Email = entity.Email ?? user.Email;
        user.IsActive = entity.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        // Update database
        await _context.SaveChangesAsync();

        return new ResponseData<User>(204, "Updated.");
      }
      catch (Exception ex)
      {
        return ResponseData<User>.Fail("Server error.", 500);
        throw new Exception(ex.Message);
      }
    }

    public async Task<ResponseData<User>> DeleteUserAsync(Guid id)
    {
      try
      {
        var userExisting = await _context.Users
            .Include(u => u.Role) // Make sure to include the Role navigation property
            .FirstOrDefaultAsync(x => x.Id == id);

        if (userExisting is null)
          return ResponseData<User>.Fail("User does not exist.", 404);

        // Check if the user has the Owner role (case-insensitive comparison)
        if (userExisting.Role != null &&
            userExisting.Role.Name.Equals("Owner", StringComparison.OrdinalIgnoreCase))
        {
          return ResponseData<User>.Fail("Cannot delete Owner accounts.", 403);
        }

        // Proceed with soft delete
        userExisting.DeletedAt = DateTime.UtcNow;
        userExisting.IsActive = false;

        await _context.SaveChangesAsync();

        return new ResponseData<User>(204, "Deleted.");
      }
      catch (Exception ex)
      {
        return ResponseData<User>.Fail("Server error.", 500);
        throw new Exception(ex.Message);
      }
    }
  }
}

