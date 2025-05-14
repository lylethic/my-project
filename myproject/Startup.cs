using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace myproject
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // Register services here
    public void ConfigureServices(IServiceCollection services, IWebHostBuilder webHost)
    {
      webHost.UseUrls("http://localhost:5001"); // Set the URL to listen on port 5001
    }

    // Configure middleware here
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
          c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
          c.RoutePrefix = string.Empty;  // Swagger UI hiển thị ở gốc (http://localhost:5001)
        });
      }
    }
  }

}
