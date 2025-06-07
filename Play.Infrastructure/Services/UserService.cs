using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Play.Application.DTOs;
using Play.Domain.Entities;
using Play.Infrastructure.Common.Caching;
using Play.Infrastructure.Common.Contracts;
using Play.Infrastructure.Common.Services;
using Play.Infrastructure.Common.Utilities;
using Play.Infrastructure.Repository;

namespace Play.Infrastructure.Services;

public class UserService(IServiceProvider services, IDbConnection connection, IRedisCacheService cache, IHttpContextAccessor httpContextAccessore, ILogger<UserService> logger) : BaseService(services), IScoped
{
    private readonly UserRepo _repo = new UserRepo(connection);
    private readonly RoleRepo _roleRepo = new RoleRepo(connection);

    public async Task CreateUserAsync(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoleId))
            throw new ArgumentException("Role ID is required.");
        if (string.IsNullOrWhiteSpace(request.FirstName))
            throw new ArgumentException("First name is required.");
        if (string.IsNullOrWhiteSpace(request.LastName))
            throw new ArgumentException("Last name is required.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required.");
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password is required.");

        var role = await _roleRepo.GetById(request.RoleId);
        if (role is null)
            throw new ArgumentException("Invalid Role ID: Role does not exist.");

        // Validate Email uniqueness if provided
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            bool isEmailUnique = await _repo.IsEmailUnique(request.Email.Trim());
            if (!isEmailUnique)
                throw new ArgumentException("Email is already in use by another user.");
        }

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            RoleId = request.RoleId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.Now,
            UpdatedAt = null,
            DeletedAt = null,
            IsActive = true
        };

        await _repo.Create(user);
    }
    public async Task UpdateUserAsync(string id, UpdateUserRequest request)
    {
        // Check if user exists
        var userExisting = await _repo.GetById(id);
        if (userExisting is null)
            throw new ArgumentException("User not found.");

        // Validate using DataAnnotations
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, context, results, true))
        {
            var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
            throw new ArgumentException($"Validation failed: {errors}");
        }

        // Check if any fields are provided for update FIRST
        bool hasUpdates = !string.IsNullOrWhiteSpace(request.FirstName) ||
                         !string.IsNullOrWhiteSpace(request.LastName) ||
                         !string.IsNullOrWhiteSpace(request.Email) ||
                         !string.IsNullOrWhiteSpace(request.RoleId) ||
                         (request.IsActive.HasValue && request.IsActive != userExisting.IsActive);

        if (!hasUpdates)
        {
            throw new ArgumentException("No fields provided for update.");
        }

        // Validate RoleId existence if provided
        if (!string.IsNullOrWhiteSpace(request.RoleId))
        {
            var roleExisting = await _roleRepo.GetById(request.RoleId);
            if (roleExisting is null)
                throw new ArgumentException("Invalid Role ID: Role does not exist.");
        }

        // Validate Email uniqueness if provided
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            bool isEmailUnique = await _repo.CheckEmailUnique(request.Email.Trim(), id);
            if (!isEmailUnique)
                throw new ArgumentException("Email is already in use by another user.");
        }

        // Update only provided fields
        if (!string.IsNullOrWhiteSpace(request.FirstName))
            userExisting.FirstName = request.FirstName;

        if (!string.IsNullOrWhiteSpace(request.LastName))
            userExisting.LastName = request.LastName;

        if (!string.IsNullOrWhiteSpace(request.Email))
            userExisting.Email = request.Email.Trim();

        if (!string.IsNullOrWhiteSpace(request.RoleId))
            userExisting.RoleId = request.RoleId;

        if (request.IsActive.HasValue)
            userExisting.IsActive = request.IsActive.Value;

        userExisting.UpdatedAt = DateTime.Now;

        try
        {
            await _repo.Update(userExisting);
        }
        catch (ArgumentException)
        {
            // Re-throw ArgumentExceptions (including our email uniqueness check)
            throw;
        }
        catch (System.Exception ex)
        {
            throw new InvalidOperationException("Failed to update user.", ex);
        }
    }
    public async Task<User> DeleteUserAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("User ID is required.");

        var userExisting = await _repo.GetById(id)
            ?? throw new KeyNotFoundException("User not found.");

        userExisting.DeletedAt = DateTime.Now;
        userExisting.IsActive = false;
        userExisting.UpdatedAt = DateTime.Now;

        var updatedUser = await _repo.Update(userExisting)
            ?? throw new InvalidOperationException("Failed to delete user.");
        return updatedUser;
    }
    public async Task<User> GetById(string id)
    {
        var user = await _repo.GetById(id)
            ?? throw new ArgumentException("Not found.");
        return user;
    }
    public async Task ImportUsersAsync(IFormFile file)
    {
        try
        {
            await _repo.ImportUsersFromExcel(file);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Error importing user.");
        }
    }
    public async Task<string> ExportUsersAsync(bool? isActive = null, int? maxRows = null)
    {
        return await _repo.ExportUsersToExcel(isActive, maxRows);
    }
    public async Task<PaginatedResponse<UserDto>> GetUsersAsync(PaginationRequest request)
    {
        var userId = httpContextAccessore.HttpContext?.User
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var cacheKey = $"users_{userId}_{request.IsActive}_{request.LastCreatedAt}_{request.PageSize}";

        // 1. Try to get from cache
        var cachedData = cache.GetData<List<UserDto>>(cacheKey);
        if (cachedData != null)
        {
            return new PaginatedResponse<UserDto>
            {
                Data = cachedData,
                PageSize = request.PageSize,
                NextCursor = cachedData.LastOrDefault()?.CreatedAt
            };
        }

        // 2. Fetch from database
        var data = await _repo.GetUsers(request);
        var result = _mapper.Map<List<UserDto>>(data);

        // 3. Cache the result (for 5 minutes)
        cache.SetData(cacheKey, result, TimeSpan.FromMinutes(5));
        logger.LogInformation($"[CACHE] Set key: {cacheKey}, value: {JsonSerializer.Serialize(data)}");

        return new PaginatedResponse<UserDto>
        {
            Data = result,
            PageSize = request.PageSize,
            NextCursor = result.LastOrDefault()?.CreatedAt
        };
    }
    public async Task<bool> IsEmailUnique(string email)
    {
        var sql = "SELECT COUNT(1) FROM users WHERE email = @Email";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email });
        return count == 0;
    }
}
