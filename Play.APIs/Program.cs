using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Play.APIs.Extensions;
using Play.APIs.Middleware;
using Play.Infrastructure.Common.Helpers;
using Play.APIs.Configuration;
using Play.Infrastructure.Common.Utilities;
using Play.Infrastructure.Common.Caching;
using Asp.Versioning;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

{
    var services = builder.Services;
    var jwtConfig = new JwtConfig();
    var envReader = new EnvReader();

    services.AddSingleton<JwtConfig>();

    // Configure Dapper global settings
    Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

    // Add Redis configuration
    builder.Services.AddRedisConfiguration(builder.Configuration);

    services.AddConnectConfiguration(builder.Configuration);
    services.AddSignalRConfiguration();

    services.AddJwtAuthentication(builder.Configuration);

    services.AddControllers().AddJsonOptions(x =>
     {
         x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
     }); ;

    services.AddApplicationServices();
    services.AddCustomAuthorization();

    services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

    // configure strongly typed settings object
    services.Configure<DbSettings>(builder.Configuration.GetSection("DbSettings"));

    services.AddResponseCaching();

    services.AddCors();

    // Add API versioning
    services.AddApiVersioning(
        options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0); // Set default API version to v1
            options.AssumeDefaultVersionWhenUnspecified = true; // Assume default version when version is not specified
            options.ReportApiVersions = true; // Expose the API version in the response headers
        })
        .AddApiExplorer(
            options =>
            {
                options.GroupNameFormat = "'v'VVV"; // Format for API version in the group name
                options.SubstituteApiVersionInUrl = true; // Substitute the API version in the URL
            }
        );

    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();

    // Add HttpContextAccessor service
    services.AddHttpContextAccessor();
    services.AddMemoryCache();

    // DI
    // services.AddSingleton<DataContext>();
    services.AddScoped<IRedisCacheService, RedisCacheService>();
    builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

    // Add logging configuration
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(); // Logs to terminal
    builder.Logging.AddDebug();
}

var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(
         options =>
    {
        var descriptions = app.DescribeApiVersions();
        foreach (var description in descriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
        }
    }
    );
}
// configure HTTP request pipeline
{
    // global cors policy
    app.UseCors(x => x
          .AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());

    // global error handler
    app.UseMiddleware<ErrorHandlerMiddleware>();

    app.UseHttpsRedirection();

    // Automatically get a token from a cookie and
    //set it in the Authorization header for every request.
    app.UseMiddleware<CookieJwtInjectorMiddleware>();

    // global error handler
    app.UseMiddleware<ErrorHandlingMiddleware>();

    app.UseAuthentication();

    app.UseAuthorization();

    app.MapControllers();

    app.UseResponseCaching();
}

app.Run();
