using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NAuth.API.Middlewares;
using NAuth.API.Services;
using NAuth.Application;
using NAuth.DTO.Settings;
using NAuth.Infra.Context;
using NAuth.Infra.Interfaces;
using zTools.DTO.Settings;
using System;
using System.Net.Mime;
using System.Text.Json;

namespace NAuth.API
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
            services.Configure<MailerSendSetting>(Configuration.GetSection("MailerSend"));
            services.Configure<NAuthSetting>(Configuration.GetSection("NAuth"));
            services.Configure<zToolsetting>(Configuration.GetSection("zTools"));

            // Tenant services
            services.AddHttpContextAccessor();
            services.AddScoped<ITenantContext, TenantContext>();
            services.AddScoped<ITenantResolver, TenantResolver>();
            services.AddTransient<TenantHeaderHandler>();
            services.AddScoped<TenantDbContextFactory>();

            // Register NAuthContext as scoped, resolved via TenantDbContextFactory
            services.AddScoped<NAuthContext>(sp =>
            {
                var factory = sp.GetRequiredService<TenantDbContextFactory>();
                return factory.CreateDbContext();
            });

            services.AddHttpClient();

            Initializer.Configure(services, Configuration);

            services.AddControllers();
            services.AddHealthChecks();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "NAuth.API",
                    Version = "v1"
                });
            });
            services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));
            services.AddHttpsRedirection(options =>
            {
                options.HttpsPort = 443;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger(c =>
                {
                    c.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0;
                });
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NAuth.API v1");
                });
            }

            app.UseHealthChecks("/",
                new HealthCheckOptions()
                {
                    ResponseWriter = async (context, report) =>
                    {
                        var result = JsonSerializer.Serialize(
                            new
                            {
                                currentTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                                statusApplication = report.Status.ToString(),
                            });

                        context.Response.ContentType = MediaTypeNames.Application.Json;
                        await context.Response.WriteAsync(result);
                    }
                });

            app.UseRouting();
            app.UseCors("MyPolicy");

            app.UseMiddleware<TenantMiddleware>();

            app.UseAuthentication();
            app.UseAuthorization();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
