using Microsoft.EntityFrameworkCore;
using Play.API.IRepository;
using Play.Application.DTOs;
using Play.Domain.Entities;
using Play.Infrastructure.Data;
using Play.Infrastructure.Helpers;
using Dapper;

namespace Play.Infrastructure.Repository
{
  public class RoleRepository : IRoleService
  {
    private readonly ApiDbContext _context;
    private readonly DataContext _dataContext;

    public RoleRepository(ApiDbContext context, DataContext dataContext)
    {
      _context = context;
      _dataContext = dataContext;
    }

    public async Task CreateRoleAsync(CreateRoleRequest role)
    {
      using var connection = _dataContext.CreateConnection();
      var roleId = string.IsNullOrWhiteSpace(role.Id) ? Guid.NewGuid().ToString() : role.Id;

      var sql = @"
                INSERT INTO roles (id, name, is_active)
                VALUES (@Id, @Name, @IsActive)";

      var parameters = new
      {
        Id = roleId,
        Name = role.Name,
        IsActive = role.IsActive
      };

      await connection.ExecuteAsync(sql, parameters);
    }

    public async Task<ResponseData<Role>> AddRoleAsync(CreateRoleDto entity)
    {
      // try
      // {
      //   var nameExisting = await _context.Roles
      //  .Where(x => x.Name.ToLower() == entity.Name.ToLower())
      //  .FirstOrDefaultAsync();

      //   if (nameExisting is not null)
      //   {
      //     return ResponseData<Role>.Fail("Role name already exists.", 400);
      //   }

      //   var role = new Role
      //   {
      //     Name = entity.Name,
      //     Description = entity.Description
      //   };

      //   await _context.Roles.AddAsync(role);
      //   await _context.SaveChangesAsync();

      //   return ResponseData<Role>.Success(role);
      // }
      // catch (Exception ex)
      // {
      //   return ResponseData<Role>.Fail("Server error.", 500);
      //   throw new Exception(ex.Message);
      // }
      throw new NotImplementedException();
    }

    public async Task<ResponseData<RoleDto>> GetRoleAsync(string id)
    {
      try
      {
        var role = await _context.Roles
        .Where(x => x.Id == id)
        .Select(r => new RoleDto { Id = r.Id, Name = r.Name, IsActive = r.IsActive })
        .FirstOrDefaultAsync();

        if (role is null) return ResponseData<RoleDto>.Fail("Not found", 404);
        return ResponseData<RoleDto>.Success(role);
      }
      catch (Exception ex)
      {
        return ResponseData<RoleDto>.Fail("Server error.", 500);
        throw new Exception(ex.Message);
      }
    }

    public async Task<ResponseData<RoleDto>> GetRolesAsync()
    {
      // try
      // {
      //   var Roles = await _context.Roles
      //     .AsNoTracking()
      //     .Select(r => new RoleDto(r.Id, r.Name, r.Description))
      //     .ToListAsync();

      //   if (Roles.Count == 0)
      //     return ResponseData<RoleDto>.Fail("No Roles found", 404);

      //   return ResponseData<RoleDto>.Success(Roles);
      // }
      // catch (Exception ex)
      // {
      //   return ResponseData<RoleDto>.Fail("Server error.", 500);
      //   throw new Exception(ex.Message);
      // }
      throw new NotImplementedException();
    }

    public async Task<ResponseData<Role>> UpdateRoleAsync(string id, UpdateRoleRequest entity)
    {
      try
      {
        var Role = await _context.Roles.FirstOrDefaultAsync(x => x.Id == id);
        if (Role is null) return ResponseData<Role>.Fail("Role not found", 404);

        Role.Name = entity.Name ?? Role.Name;
        Role.IsActive = entity.IsActive;

        _context.Roles.Update(Role);
        await _context.SaveChangesAsync();

        return ResponseData<Role>.Success(Role);
      }
      catch (Exception ex)
      {
        return ResponseData<Role>.Fail("Server error.", 500);
        throw new Exception(ex.Message);
      }
    }

    public async Task<ResponseData<Role>> DeleteRoleAsync(string id)
    {
      var RoleExisting = await _context.Roles.Where(x => x.Id == id).FirstOrDefaultAsync();
      if (RoleExisting is null)
      {
        return ResponseData<Role>.Fail("Role Not found", 404);
      }

      _context.Roles.Remove(RoleExisting);
      await _context.SaveChangesAsync();
      return ResponseData<Role>.Success(RoleExisting);
    }
  }
}
