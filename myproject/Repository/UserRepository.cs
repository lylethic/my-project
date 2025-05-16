using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using myproject.Data;
using myproject.DTOs;
using myproject.Entities;
using myproject.Helpers;
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

    public async Task<ResponseData<PaginatedResponse<UserDto>>> GetUsersAsync(QueryParameters parameters)
    {
      try
      {
        // Base query
        var query = _context.Users.AsQueryable();

        // Apply filtering
        if (parameters.IsActive.HasValue)
          query = query.Where(u => u.IsActive == parameters.IsActive.Value);

        // Apply search if SearchTerm is provided
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
          var searchTerm = parameters.SearchTerm.ToLower();
          query = query.Where(u =>
              u.Name.ToLower().Contains(searchTerm) ||
              u.Email.ToLower().Contains(searchTerm)
          );
        }

        // Calculate total records BEFORE pagination
        int totalRecords = await query.CountAsync();

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(parameters.SortBy))
        {
          query = parameters.SortBy.ToLower() switch
          {
            "name" => parameters.SortDescending
                     ? query.OrderByDescending(u => u.Name)
                     : query.OrderBy(u => u.Name),
            "email" => parameters.SortDescending
                      ? query.OrderByDescending(u => u.Email)
                      : query.OrderBy(u => u.Email),
            "createdat" => parameters.SortDescending
                          ? query.OrderByDescending(u => u.CreatedAt)
                          : query.OrderBy(u => u.CreatedAt),
            _ => parameters.SortDescending
                 ? query.OrderByDescending(u => u.Id)
                 : query.OrderBy(u => u.Id) // Default sort by Id
          };
        }
        else
        {
          // Default sorting by Id if no sort field specified
          query = query.OrderBy(u => u.Id);
        }

        // Apply pagination
        var users = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(u => new UserDto(
                u.Id,
                u.RoleId,
                u.Name,
                u.Email
            ))
            .ToListAsync();

        // Create paginated response
        var paginatedResponse = new PaginatedResponse<UserDto>
        {
          Items = users,
          TotalItems = totalRecords,
          PageNumber = parameters.Page,
          PageSize = parameters.PageSize
        };

        // If no users found
        if (users.Count == 0)
          return ResponseData<PaginatedResponse<UserDto>>.Fail("No users found.", 404);

        return ResponseData<PaginatedResponse<UserDto>>.Success(paginatedResponse);
      }
      catch (DbUpdateException dbEx)
      {
        throw new AppException($"Database error occurred while retrieving users: {dbEx.Message}");
      }
      catch (InvalidOperationException ioEx)
      {
        throw new AppException($"Invalid operation while retrieving users: {ioEx.Message}");
      }
      catch (Exception ex)
      {
        // Log the exception
        return ResponseData<PaginatedResponse<UserDto>>.Fail("Error retrieving users", 500);
        throw new AppException($"An error occurred while retrieving users: {ex.Message}");
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
        if (!Utils.IsValidEmail(entity.Email))
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

