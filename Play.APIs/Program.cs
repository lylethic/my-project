using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Play.APIs.Extensions;
using Play.APIs.Middleware;
using Play.Infrastructure.Common.Helpers;
using System.Data;
using System.Text;
using System.Text.Json.Serialization;
using Play.APIs.Configuration;
using Play.Infrastructure.Common.Utilities;
using Play.Infrastructure.Common.Caching;

var builder = WebApplication.CreateBuilder(args);

{
    var services = builder.Services;
    var env = builder.Environment;
    var jwtConfig = new JwtConfig();
    var envReader = new EnvReader();
    var emailSys = new EmailSys(
   host: envReader.GetString("SMTP_HOST")!,
   port: int.Parse(envReader.GetString("SMTP_PORT")!),
   user: envReader.GetString("SMTP_USER")!,
   pass: envReader.GetString("SMTP_PASS")!
   );

    // Add services to the container.
    var conn = builder.Configuration.GetConnectionString("DefaultConnection");

    services.AddSingleton<JwtConfig>();

    // Configure Dapper global settings
    Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

    services.AddScoped<IDbConnection>(provider =>
      {
          var connectionString = envReader.GetString("DB_CONNECTION") ?? conn;
          return new Npgsql.NpgsqlConnection(connectionString);
      });

    services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "PlayRedisInstance";
    });

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

    // Add authorization with policy
    services.AddAuthorization(options =>
     {
         options.AddPolicy("RequireOwnerAdminRole", policy => policy.RequireRole("owner", "admin"));
         options.AddPolicy("RequireOwnerRole", policy => policy.RequireRole("owner"));
         options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("admin"));
         options.AddPolicy("RequireUserRole", policy => policy.RequireRole("user"));
     });

    services.AddControllers().AddJsonOptions(x =>
     {
         // serialize enums as strings in api responses (e.g. Role)
         x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
     }); ;
    // Register ALL your application services at once!
    services.AddApplicationServices();

    services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

    // configure strongly typed settings object
    services.Configure<DbSettings>(builder.Configuration.GetSection("DbSettings"));

    services.AddResponseCaching();

    services.AddCors();

    // Add API versioning
    services.AddApiVersioning(options =>
     {
         options.DefaultApiVersion = new ApiVersion(1, 0); // Set default API version to v1
         options.AssumeDefaultVersionWhenUnspecified = true; // Assume default version when version is not specified
         options.ReportApiVersions = true; // Expose the API version in the response headers
     });

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
     {
         c.SwaggerDoc("v1", new OpenApiInfo
         {
             Title = "My API",
             Version = "v1"
         });

         // Add Bearer token support
         c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
         {
             In = ParameterLocation.Header,
             Description = "Please enter a valid token",
             Name = "Authorization",
             Type = SecuritySchemeType.Http,
             BearerFormat = "JWT",
             Scheme = "Bearer"
         });

         // Add security requirement for Bearer token
         c.AddSecurityRequirement(new OpenApiSecurityRequirement
         {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
         });
     });

    // Add HttpContextAccessor service
    services.AddHttpContextAccessor();
    services.AddMemoryCache();

    // DI
    services.AddSingleton<DataContext>();
    services.AddScoped<IRedisCacheService, RedisCacheService>();

    services.AddSingleton<IEmailService>(emailSys);


    // Add logging configuration
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(); // Logs to terminal
    builder.Logging.AddDebug();
}

var app = builder.Build();
// ensure database and tables exist
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<DataContext>();
    await context.Init();
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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
