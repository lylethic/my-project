using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using myproject.Data;
using myproject.IRepository;
using myproject.Repository;
using System.Text;
using System.Text.Json;
using Npgsql;
using myproject.Helpers;
using System.Text.Json.Serialization;
using myproject.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var conn = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApiDbContext>(options =>
{
  options.UseNpgsql(conn);

  // Add detailed logging
  options.EnableSensitiveDataLogging();
  options.LogTo(Console.WriteLine, LogLevel.Information);
});

builder.Services.AddAuthentication(options =>
{
  options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
  options.TokenValidationParameters = new TokenValidationParameters
  {
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = builder.Configuration["AuthConfiguration:Issuer"],
    ValidAudience = builder.Configuration["AuthConfiguration:Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(
          Encoding.UTF8.GetBytes(builder.Configuration["AuthConfiguration:Key"]!))
  };
});

// Add authorization with policy
builder.Services.AddAuthorization(options =>
{
  options.AddPolicy("RequireOwnerAdminRole", policy => policy.RequireRole("owner", "admin"));
  options.AddPolicy("RequireOwnerRole", policy => policy.RequireRole("owner"));
  options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("admin"));
  options.AddPolicy("RequireUserRole", policy => policy.RequireRole("user"));
});

builder.Services.AddControllers().AddJsonOptions(x =>
    {
      // serialize enums as strings in api responses (e.g. Role)
      x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }); ;

builder.Services.AddCors();

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
  options.DefaultApiVersion = new ApiVersion(1, 0); // Set default API version to v1
  options.AssumeDefaultVersionWhenUnspecified = true; // Assume default version when version is not specified
  options.ReportApiVersions = true; // Expose the API version in the response headers
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
  c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
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
builder.Services.AddHttpContextAccessor();

// DI
builder.Services.AddScoped<IAuth, AuthRepository>();
builder.Services.AddScoped<IUserService, UserRepository>();
builder.Services.AddScoped<IProductService, ProductRepository>();
builder.Services.AddScoped<IRoleService, RoleRepository>();

// Set the URL to listen on port 5001 https://localhost:5001
// builder.WebHost.UseUrls("https://localhost:5001");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

// global error handler
app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseHttpsRedirection();

// Automatically get a token from a cookie and
//set it in the Authorization header for every request.
app.UseMiddleware<CookieJwtInjectorMiddleware>();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
