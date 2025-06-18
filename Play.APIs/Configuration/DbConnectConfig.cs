using System;
using System.Data;
using Play.Infrastructure.Common.Utilities;

namespace Play.APIs.Configuration;

public static class DbConnectConfig
{
    public static IServiceCollection AddDbConnectConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Add services to the container.
        var conn = configuration.GetConnectionString("DefaultConnection");
        var envReader = new EnvReader();
        services.AddScoped<IDbConnection>(provider =>
              {
                  var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION") ?? conn;
                  return new Npgsql.NpgsqlConnection(connectionString);
              });
        return services;
    }
}
