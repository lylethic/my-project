using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using myproject.Data;
using myproject.DTOs;
using myproject.Entities;
using myproject.IRepository;

namespace myproject.Repository
{
  public class UserRepository : IUserService
  {
    private readonly ApiDbContext _context;

    public UserRepository(ApiDbContext context)
    {
      this._context = context;
    }

    private readonly PasswordHasher<User> _passwordHasher = new();

    // Separate method for hashing password
    private string HashPassword(User user, string plainPassword)
    {
      return _passwordHasher.HashPassword(user, plainPassword);
    }

    // Separate method for verifying password
    public bool VerifyPassword(User user, string inputPassword)
    {
      var result = _passwordHasher.VerifyHashedPassword(user, user.Password, inputPassword);
      return result == PasswordVerificationResult.Success;
    }

    public async Task<ResponseData<User>> GetUsersAsync(bool? isActive = true)
    {
      var users = await _context.Users
      .Where(u => u.IsActive == isActive)
      .ToListAsync();

      if (users.Count == 0)
        return ResponseData<User>.Fail("No users found.", 404);

      return ResponseData<User>.Success(users);
    }

    public async Task<ResponseData<User>> GetUserAsync(Guid id)
    {
      var user = await _context.Users.FindAsync(id);
      if (user == null) return ResponseData<User>.Fail("User does not exist.", 404);
      return ResponseData<User>.Success(user);
    }

    public async Task<ResponseData<User>> AddUserAsync(CreateUserDto entity)
    {
      var query = await _context.Users.Where(x => x.Email == entity.Email).FirstOrDefaultAsync();
      if (query is not null) return ResponseData<User>.Fail("Email existed.", 400);

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
      _context.SaveChanges();

      return ResponseData<User>.Success(user);
    }

    public async Task<ResponseData<User>> UpdateUserAsync(Guid id, UpdateUserDto entity)
    {
      var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
      if (user is null) return ResponseData<User>.Fail("User not found!", 404);

      user.RoleId = entity.RoleId;
      user.Name = entity.Name ?? user.Name;
      user.Email = entity.Email ?? user.Email;
      user.UpdatedAt = DateTime.UtcNow;

      // Update database
      _context.SaveChanges();

      return new ResponseData<User>(204, "Updated.");
    }

    public async Task<ResponseData<User>> DeleteUserAsync(Guid id)
    {
      var userExisting = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
      if (userExisting is null) return ResponseData<User>.Fail("User does not exist.", 404);

      userExisting.DeletedAt = DateTime.UtcNow;
      userExisting.IsActive = false;

      _context.SaveChanges();

      return new ResponseData<User>(204, "Deleted.");
    }
  }
}
