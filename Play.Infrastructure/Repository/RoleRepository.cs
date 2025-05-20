using Microsoft.EntityFrameworkCore;
using Play.API.IRepository;
using Play.Application.DTOs;
using Play.Domain.Entities;
using Play.Infrastructure.Data;

namespace Play.Infrastructure.Repository
{
  public class RoleRepository : IRoleService
  {
    private readonly ApiDbContext _context;

    public RoleRepository(ApiDbContext context)
    {
      _context = context;
    }

    public async Task<ResponseData<Role>> AddRoleAsync(CreateRoleDto entity)
    {
      try
      {
        var nameExisting = await _context.Roles
       .Where(x => x.Name.ToLower() == entity.Name.ToLower())
       .FirstOrDefaultAsync();

        if (nameExisting is not null)
        {
          return ResponseData<Role>.Fail("Role name already exists.", 400);
        }

        var role = new Role
        {
          Name = entity.Name,
          Description = entity.Description
        };

        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();

        return ResponseData<Role>.Success(role);
      }
      catch (Exception ex)
      {
        return ResponseData<Role>.Fail("Server error.", 500);
        throw new Exception(ex.Message);
      }
    }

    public async Task<ResponseData<RoleDto>> GetRoleAsync(Guid id)
    {
      try
      {
        var role = await _context.Roles
        .Where(x => x.Id == id)
        .Select(r => new RoleDto(r.Id, r.Name, r.Description))
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
      try
      {
        var Roles = await _context.Roles
          .AsNoTracking()
          .Select(r => new RoleDto(r.Id, r.Name, r.Description))
          .ToListAsync();

        if (Roles.Count == 0)
          return ResponseData<RoleDto>.Fail("No Roles found", 404);

        return ResponseData<RoleDto>.Success(Roles);
      }
      catch (Exception ex)
      {
        return ResponseData<RoleDto>.Fail("Server error.", 500);
        throw new Exception(ex.Message);
      }
    }

    public async Task<ResponseData<Role>> UpdateRoleAsync(Guid id, UpdateRoleDto entity)
    {
      try
      {
        var Role = await _context.Roles.FirstOrDefaultAsync(x => x.Id == id);
        if (Role is null) return ResponseData<Role>.Fail("Role not found", 404);

        Role.Name = entity.Name ?? Role.Name;
        Role.Description = entity.Description ?? Role.Description;

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

    public async Task<ResponseData<Role>> DeleteRoleAsync(Guid id)
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
