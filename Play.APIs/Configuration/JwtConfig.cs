using System;
using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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
        Env.Load();

        Secret = Environment.GetEnvironmentVariable("API_SECRET")
                 ?? throw new ArgumentNullException("API_SECRET is not set.");
        Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
                 ?? throw new ArgumentNullException("JWT_ISSUER is not set.");
        Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                 ?? throw new ArgumentNullException("JWT_AUDIENCE is not set.");

        // Safe parsing with fallback default values
        if (!int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRY_HOURS"), out int expiry))
        {
            expiry = 1; // default to 1 hour if not set
        }

        if (!int.TryParse(Environment.GetEnvironmentVariable("JWT_REFRESH_EXPIRY_HOURS"), out int refreshExpiry))
        {
            refreshExpiry = 24; // default to 24 hours if not set
        }

        ExpiryHours = expiry;
        RefreshExpiryHours = refreshExpiry;
    }
}

public static class AuthenticationConfig
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
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
                    var cookieName = Environment.GetEnvironmentVariable("ACCESSTOKEN_COOKIENAME");
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
