using HealthCheckSample.CustomHealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthCheckSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "HealthCheckSample", Version = "v1" });
            });

            services.AddHealthChecks()
            .AddAsyncCheck("custom", async () => await Task.FromResult(new HealthCheckResult(HealthStatus.Degraded, "this service is not working but doesn't block the project")))
            //.AddCheck("custom1", () => new HealthCheckResult(HealthStatus.Unhealthy, "this service is not working and block the project"))
            .AddCheck<BasicHealthCheck>("custom2");

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HealthCheckSample v1"));
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/canary", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions()
                {
                    ResponseWriter = async (context, report) =>
                    {
                        context.Response.ContentType = "application/json";
                        var response = new HealthCheckReponse
                        {
                            Status = report.Status.ToString(),
                            Services = report.Entries.Select(x => new HealthCheckService
                            {
                                Name = x.Key,
                                Status = x.Value.Status.ToString(),
                                Description = x.Value.Description
                            })
                        };
                        await context.Response.WriteAsync( System.Text.Json.JsonSerializer.Serialize(response));
                    }
                });
                endpoints.MapControllers();
            });
        }
    }

    public class HealthCheckReponse
    {
        public string Status { get; set; }
        public IEnumerable<HealthCheckService> Services { get; set; }
    }

    public class HealthCheckService
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
    }
}
