using System.Security.Claims;

namespace myproject.Helper
{
  public static class Helper
  {
    // Create this in a utilities 
    public static Guid? GetRoleId(this ClaimsPrincipal user)
    {
      if (user?.Identity?.IsAuthenticated != true)
      {
        return null;
      }

      var roleIdClaim = user.Claims.FirstOrDefault(c => c.Type == "RoleId");

      if (roleIdClaim != null && Guid.TryParse(roleIdClaim.Value, out Guid roleId))
      {
        return roleId;
      }

      return null;
    }
  }

  // Then use it like this in any controller or service
  //var roleId = User.GetRoleId();
}

