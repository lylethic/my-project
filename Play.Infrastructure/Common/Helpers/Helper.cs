using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Play.Infrastructure.Common.Helpers;

public static class Utils
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

  // Then use it like this in any controller or service
  //var roleId = User.GetRoleId();

  public static bool IsValidEmail(string email)
  {
    if (string.IsNullOrWhiteSpace(email))
      return false;

    try
    {
      // Normalize the domain
      email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                            RegexOptions.None, TimeSpan.FromMilliseconds(200));

      // Examines the domain part of the email and normalizes it.
      static string DomainMapper(Match match)
      {
        // Use IdnMapping class to convert Unicode domain names.
        var idn = new IdnMapping();

        // Pull out and process domain name (throws ArgumentException on invalid)
        string domainName = idn.GetAscii(match.Groups[2].Value);

        return match.Groups[1].Value + domainName;
      }
    }
    catch (RegexMatchTimeoutException)
    {
      return false;
    }
    catch (ArgumentException)
    {
      return false;
    }

    try
    {
      return Regex.IsMatch(email,
          @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
          RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
    }
    catch (RegexMatchTimeoutException)
    {
      return false;
    }
  }
}



