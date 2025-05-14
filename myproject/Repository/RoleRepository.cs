using Microsoft.EntityFrameworkCore;
using myproject.Data;
using myproject.DTOs;
using myproject.Entities;
using myproject.IRepository;

namespace myproject.Repository
{
  public class RoleRepository : IRoleService
  {
    private readonly ApiDbContext _context;

    public RoleRepository(ApiDbContext context)
    {
      this._context = context;
    }

    public async Task<ResponseData<Role>> AddRoleAsync(CreateRoleDto entity)
    {
      var nameExisting = await _context.Roles
        .Where(x => x.Name.ToLower() == entity.Name.ToLower())
        .FirstOrDefaultAsync();

      if (nameExisting is not null)
      {
        return ResponseData<Role>.Fail("Role name already exists.", 400);
      }

      var Role = new Role
      {
        Name = entity.Name,
        Description = entity.Description
      };

      await _context.Roles.AddAsync(Role);
      await _context.SaveChangesAsync();

      return ResponseData<Role>.Success(Role);
    }


    public async Task<ResponseData<Role>> GetRoleAsync(Guid id)
    {
      var find = await _context.Roles.Where(x => x.Id == id).FirstOrDefaultAsync();
      if (find is null) return ResponseData<Role>.Fail("Not found", 404);
      return ResponseData<Role>.Success(find);
    }

    public async Task<ResponseData<Role>> GetRolesAsync()
    {
      var Roles = await _context.Roles.ToListAsync();
      if (Roles.Count == 0)
        return ResponseData<Role>.Fail("No Roles found", 404);

      return ResponseData<Role>.Success(Roles);
    }

    public async Task<ResponseData<Role>> UpdateRoleAsync(Guid id, UpdateRoleDto entity)
    {
      var Role = await _context.Roles.FirstOrDefaultAsync(x => x.Id == id);
      if (Role is null) return ResponseData<Role>.Fail("Role not found", 404);

      Role.Name = entity.Name ?? Role.Name;
      Role.Description = entity.Description ?? Role.Description;

      _context.Roles.Update(Role);
      _context.SaveChanges();

      return ResponseData<Role>.Success(Role);
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
