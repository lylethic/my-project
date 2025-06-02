using System;
using Play.Infrastructure.Common.Utilities;

namespace Play.APIs.Configuration;

public class JwtConfig
{
    public string Secret { get; }
    public string Issuer { get; }
    public string Audience { get; }
    public int ExpiryHours { get; }
    public int RefreshExpiryHours { get; }

    public JwtConfig()
    {
        var envReader = new EnvReader();
        Secret = envReader.GetString("API_SECRET");
        Issuer = envReader.GetString("JWT_ISSUER");
        Audience = envReader.GetString("JWT_AUDIENCE");
        ExpiryHours = envReader.GetInt("JWT_EXPIRY_HOURS");
        RefreshExpiryHours = envReader.GetInt("JWT_REFRESH_EXPIRY_HOURS");
    }
}
