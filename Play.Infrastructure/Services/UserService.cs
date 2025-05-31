using System;
using System.Data;
using Play.Application.DTOs;
using Play.Domain.Entities;
using Play.Infrastructure.Common.Contracts;
using Play.Infrastructure.Common.Services;
using Play.Infrastructure.Repository;

namespace Play.Infrastructure.Services;

public class UserService(IServiceProvider services, IDbConnection connection) : BaseService(services), IScoped
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
        if (role == null)
            throw new ArgumentException("Invalid Role ID: Role does not exist.");

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

    public async Task<UserDto> GetById(string id)
    {
        var user = await _repo.GetById(id)
            ?? throw new Exception("Not found.");
        return user;
    }
}
