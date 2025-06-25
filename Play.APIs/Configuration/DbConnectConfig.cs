using System.Data;
using Npgsql;

namespace Play.APIs.Configuration;

public static class DbConnectConfig
{
    public static IServiceCollection AddDbConnectConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var conn = configuration.GetConnectionString("DefaultConnection");

        services.AddScoped<IDbConnection>(provider =>
        {
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Database");
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION") ?? conn;

            var connection = new NpgsqlConnection(connectionString);

            try
            {
                connection.Open();
                logger.LogInformation("Database connection established successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to the database.");
                throw; // Optionally rethrow if you want it to fail fast
            }

            // close the connection now if it was just for testing
            connection.Close();

            return new NpgsqlConnection(connectionString); // Return new one for DI
        });

        return services;
    }
}
