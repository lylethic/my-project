using System.Text.Json.Serialization;
using Play.APIs.Middleware;
using Play.Infrastructure.Common.Helpers;
using Play.APIs.Configuration;
using Play.Infrastructure.Common.Caching;
using Asp.Versioning;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Play.APIs.Common;
using Play.Infrastructure.Common.Mail;
using DotNetEnv;
using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);
{
    Env.Load();
    var services = builder.Services;
    var jwtConfig = new JwtConfig();

    services.AddSingleton<JwtConfig>();

    // Configure Dapper global settings
    Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

    // Add Redis configuration
    services.AddRedisConfiguration(builder.Configuration);

    services.AddDbConnectConfiguration(builder.Configuration);
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

    // Configure Gmail options from environment variables
    services.Configure<GmailOptions>(options =>
    {
        options.Host = Environment.GetEnvironmentVariable("GMAIL_HOST");
        options.Port = int.Parse(Environment.GetEnvironmentVariable("GMAIL_PORT"));
        options.Email = Environment.GetEnvironmentVariable("GMAIL_EMAIL") ?? "";
        options.Password = Environment.GetEnvironmentVariable("GMAIL_PASSWORD") ?? "";
    });

    // Add HttpContextAccessor service
    services.AddHttpContextAccessor();
    services.AddMemoryCache();

    // DI
    // services.AddSingleton<DataContext>();
    services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

    services.AddResponseCaching();

    services.AddCors();

    // Add logging configuration
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(); // Logs to terminal
    builder.Logging.AddDebug();

    services.AddHttpLogging(logging =>
    {
        logging.LoggingFields = HttpLoggingFields.All;
    });
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

    app.UseOutputCache();

    // global error handler
    app.UseMiddleware<ErrorHandlerMiddleware>();

    app.UseHttpsRedirection();

    // Automatically get a token from a cookie and
    //set it in the Authorization header for every request.
    app.UseMiddleware<CookieJwtInjectorMiddleware>();

    app.UseMiddleware<InterceptorHttpLoggingMiddleware>();

    // global error handler
    app.UseMiddleware<ErrorHandlingMiddleware>();

    app.UseAuthentication();

    app.UseAuthorization();

    app.MapControllers();
}

app.Run();
