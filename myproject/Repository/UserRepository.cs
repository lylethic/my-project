using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private readonly ILogger<UserRepository> _logger;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public UserRepository(ApiDbContext context, IMemoryCache cache, IConfiguration configuration, ILogger<UserRepository> logger)
    {
      this._context = context;
      this._cache = cache;
      this._config = configuration;
      this._logger = logger;

      _cacheOptions = new MemoryCacheEntryOptions()
               .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
               .SetSlidingExpiration(TimeSpan.FromMinutes(2));
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
            .AsNoTracking()
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

    public async Task<ResponseData<User>> GetUserAsync(Guid id)
    {
      var stopwatch = new Stopwatch();
      stopwatch.Start();
      string cacheKey = $"user-{id}";

      if (_cache.TryGetValue(cacheKey, out User? cachedUser)) // Notice the type UserDto?
      {
        stopwatch.Stop();
        _logger.LogInformation("User {UserId} found in cache. Time taken: {ElapsedMilliseconds}ms", id, stopwatch.ElapsedMilliseconds);
        if (cachedUser != null) // It's possible to cache a null if you want to cache "not found"
        {
          return ResponseData<User>.Success(cachedUser);
        }
        // If you cache "not found" as null, you might want to return a specific response here.
        // For this example, we assume only successfully retrieved users are cached.
      }

      _logger.LogInformation("User {UserId} not found in cache. Fetching from database.", id);

      try
      {
        var user = await _context.Users
            .Where(u => u.Id == id)
            .AsNoTracking()
            .Include(x => x.Role)
            .FirstOrDefaultAsync();

        stopwatch.Stop();

        if (user == null)
        {
          _logger.LogWarning("User {UserId} not found in database. Time taken: {ElapsedMilliseconds}ms", id, stopwatch.ElapsedMilliseconds);
          return ResponseData<User>.Fail("User not found", 404);
        }

        // 3. Add the fetched user to the cache
        _cache.Set(cacheKey, user, _cacheOptions);
        _logger.LogInformation("User {UserId} fetched from database and cached. Time taken: {ElapsedMilliseconds}ms", id, stopwatch.ElapsedMilliseconds);

        return ResponseData<User>.Success(user);
      }
      catch (NpgsqlException ex) // Be specific with database exceptions if possible
      {
        stopwatch.Stop();
        _logger.LogError(ex, "Database error while fetching user {UserId}. Time taken: {ElapsedMilliseconds}ms", id, stopwatch.ElapsedMilliseconds);
        return ResponseData<User>.Fail("Database error", 500);
      }
      catch (Exception ex)
      {
        stopwatch.Stop();
        _logger.LogError(ex, "An error occurred while fetching user {UserId}. Time taken: {ElapsedMilliseconds}ms", id, stopwatch.ElapsedMilliseconds);
        return ResponseData<User>.Fail("An error occurred", 500);
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

        // Single roundtrip query
        var roleCheck = await _context.Roles
            .Where(r => r.Id == entity.RoleId)
            .Select(r => new
            {
              RoleExists = true,
              EmailExists = _context.Users.Any(u => u.Email == entity.Email)
            })
            .FirstOrDefaultAsync();

        var roleExists = roleCheck?.RoleExists ?? false;
        var emailExists = roleCheck?.EmailExists ?? false;

        if (!roleExists) return ResponseData<User>.Fail("Role not found", 404);
        if (emailExists) return ResponseData<User>.Fail("Email existed.", 400);

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

    public async Task<ResponseData<List<User>>> AddUsersAsync(IEnumerable<CreateUserDto> entities)
    {
      try
      {
        // Validate all emails first
        var invalidEmails = entities
            .Where(e => !Utils.IsValidEmail(e.Email))
            .Select(e => e.Email)
            .ToList();

        if (invalidEmails.Any())
        {
          return ResponseData<List<User>>.Fail(
              $"Invalid email format for: {string.Join(", ", invalidEmails)}",
              400);
        }

        // Check for duplicate emails in the batch
        var duplicateEmails = entities
            .GroupBy(e => e.Email)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateEmails.Any())
        {
          return ResponseData<List<User>>.Fail(
              $"Duplicate emails in batch: {string.Join(", ", duplicateEmails)}",
              400);
        }

        // Get all emails from the batch and existing emails in single query
        var batchEmails = entities.Select(e => e.Email).ToList();
        var existingEmails = await _context.Users
            .Where(u => batchEmails.Contains(u.Email))
            .Select(u => u.Email)
            .ToListAsync();

        // Check for existing emails
        var conflictingEmails = batchEmails.Intersect(existingEmails).ToList();
        if (conflictingEmails.Any())
        {
          return ResponseData<List<User>>.Fail(
              $"Emails already exist: {string.Join(", ", conflictingEmails)}",
              400);
        }

        // Verify all roles exist in single query
        var roleIds = entities.Select(e => e.RoleId).Distinct().ToList();
        var existingRoleIds = await _context.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync();

        var missingRoleIds = roleIds.Except(existingRoleIds).ToList();
        if (missingRoleIds.Any())
        {
          return ResponseData<List<User>>.Fail(
              $"Roles not found: {string.Join(", ", missingRoleIds)}",
              404);
        }

        // Create users
        var users = new List<User>();
        foreach (var entity in entities)
        {
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
          users.Add(user);
        }

        // Bulk insert (more efficient than individual inserts)
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        return ResponseData<List<User>>.Success(201, "Successfully added users.");
      }
      catch (Exception ex)
      {
        // Log the actual error but return generic message
        _logger.LogError(ex, "Error adding multiple users");
        return ResponseData<List<User>>.Fail("Server error while processing batch", 500);
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

    public async Task<ResponseData<int>> AddUsersFromExcelAsync(Stream excelStream)
    {
      try
      {
        var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.First();

        int addedCount = 0;
        var rows = worksheet.RowsUsed().Skip(1); // Skip header row

        foreach (var row in rows)
        {
          var roleIdCell = row.Cell(1).GetString();
          var name = row.Cell(2).GetString();
          var email = row.Cell(3).GetString();
          var password = row.Cell(4).GetString();

          if (!Guid.TryParse(roleIdCell, out Guid roleId))
          {
            _logger.LogWarning($"Invalid RoleId '{roleIdCell}' at row {row.RowNumber()}");
            continue;
          }

          var userDto = new CreateUserDto(roleId, name, email, password);

          var result = await AddUserAsync(userDto);
          if (result.StatusCode == 200)
          {
            addedCount++;
          }
          else
          {
            _logger.LogWarning($"Failed to add user at row {row.RowNumber()}: {result.Message}");
          }
        }

        return ResponseData<int>.Success(200, $"{addedCount} users added successfully.");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error importing users from Excel.");
        return ResponseData<int>.Fail("Error importing users.", 500);
      }
    }

    public async Task<byte[]> ExportUsersToExcelAsync(int take = 20, Guid? roleId = null)
    {
      await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

      try
      {
        var query = _context.Users.AsQueryable();

        if (roleId.HasValue)
        {
          query = query.Where(u => u.RoleId == roleId.Value);
        }

        var users = await query
            .Include(u => u.Role)
            .OrderByDescending(u => u.CreatedAt)
            .Take(take)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Users");

        // Header row
        worksheet.Cell(1, 1).Value = "Id";
        worksheet.Cell(1, 2).Value = "Name";
        worksheet.Cell(1, 3).Value = "Email";
        worksheet.Cell(1, 4).Value = "Role";

        int row = 2;
        foreach (var user in users)
        {
          worksheet.Cell(row, 1).Value = user.Id.ToString();
          worksheet.Cell(row, 2).Value = user.Name;
          worksheet.Cell(row, 3).Value = user.Email;
          worksheet.Cell(row, 4).Value = user.Role?.Name ?? user.RoleId.ToString();
          row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        // Commit the transaction (even though it's read-only, this confirms scope ends cleanly)
        await transaction.CommitAsync();

        return stream.ToArray();
      }
      catch (Exception ex)
      {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Error exporting users to Excel.");
        throw new AppException($"Error exporting users to Excel: {ex.Message}");
      }
    }

  }
}

