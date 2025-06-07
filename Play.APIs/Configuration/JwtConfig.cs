using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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

public static class AuthenticationConfig
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var envReader = new EnvReader();
        var jwtConfig = new JwtConfig();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var cookieName = envReader.GetString("ACCESSTOKEN_COOKIENAME");
                    var accessToken = context.Request.Cookies[cookieName];
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtConfig.Issuer,
                ValidAudience = jwtConfig.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret))
            };
        });

        return services;
    }

    public static IServiceCollection AddCustomAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireOwnerAdminRole", policy => policy.RequireRole("owner", "admin"));
            options.AddPolicy("RequireOwnerRole", policy => policy.RequireRole("owner"));
            options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("admin"));
            options.AddPolicy("RequireUserRole", policy => policy.RequireRole("user"));
        });

        return services;
    }
}
