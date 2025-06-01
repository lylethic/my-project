using System.Data;
using DocumentFormat.OpenXml.Math;
using Play.Application.DTOs;
using Play.Domain.Entities;
using Play.Infrastructure.Common.Contracts;
using Play.Infrastructure.Common.Services;
using Play.Infrastructure.Repository;

namespace Play.Infrastructure.Services;

public class RoleService(IServiceProvider services, IDbConnection connection) : BaseService(services), IScoped
{
    private readonly RoleRepo _repo = new RoleRepo(connection);

    public async Task CreateRoleAsync(CreateRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Role name is required.");

        var role = new Role
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            CreatedAt = DateTime.Now,
            UpdatedAt = null,
            DeletedAt = null,
            IsActive = true
        };

        await _repo.Create(role);
    }

    public async Task<Role> UpdateRoleAsync(string id, UpdateRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Role ID is required.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Role name is required.");

        var roleExisting = await _repo.GetById(id) ?? throw new KeyNotFoundException("Role not found.");

        // Map request to existing role
        _mapper.Map(request, roleExisting);

        // Ensure UpdatedAt is set
        roleExisting.UpdatedAt = DateTime.Now;
        var updatedRole = await _repo.Update(roleExisting);
        if (updatedRole == null)
            throw new InvalidOperationException("Failed to update the role.");

        return updatedRole;
    }

    public async Task<Role> DeleteRoleAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Role ID is required.");

        var roleExisting = await _repo.GetById(id)
            ?? throw new KeyNotFoundException("Role not found.");

        roleExisting.DeletedAt = DateTime.Now;
        roleExisting.IsActive = false;
        roleExisting.UpdatedAt = DateTime.Now;

        var updatedRole = await _repo.Update(roleExisting)
            ?? throw new InvalidOperationException("Failed to delete the role.");
        return updatedRole;
    }

    // Admin
    public async Task DeleteRole(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new KeyNotFoundException("Role ID is required.");
        var role = await _repo.GetById(id) ?? throw new KeyNotFoundException("Role not found.");
        await _repo.Delete(role);
    }

    public async Task<PaginatedResponse<RoleDto>> GetPaginatedRolesAsync(PaginationRequest request)
    {
        var data = await _repo.GetRolesWithPagination(request);
        var result = _mapper.Map<List<RoleDto>>(data);
        var nextCursor = result.LastOrDefault()?.CreatedAt;
        return new PaginatedResponse<RoleDto>
        {
            Data = result,
            PageSize = request.PageSize,
            NextCursor = nextCursor
        };
    }

    public async Task<Role> GetById(string id)
    {
        var role = await _repo.GetById(id) ?? throw new Exception("Not found.");
        return role;
    }
}