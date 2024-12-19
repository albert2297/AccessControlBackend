using AccessControl.Infrastructure.Persistence.DbContext;
using AccessControl.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

namespace AccessControl.Infrastructure.Configuration
{
    public static class ConfigurationHelper
    {
        public static void ConfigureIdentityAndDbContext(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AccessControlDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<UserEntity, IdentityRole<Guid>>()
                .AddEntityFrameworkStores<AccessControlDbContext>()
                .AddDefaultTokenProviders();
        }
        public static void ReplaceEnvironmentVariables(IConfiguration configuration)
        {
            foreach (var kvp in configuration.AsEnumerable())
            {
                if (kvp.Value?.Contains("${") == true)
                {
                    var startIndex = kvp.Value.IndexOf('{') + 1;
                    var endIndex = kvp.Value.IndexOf('}', startIndex);
                    var envVarName = kvp.Value.AsSpan(startIndex, endIndex - startIndex);
                    var envVarValue = Environment.GetEnvironmentVariable(envVarName.ToString());

                    if (!string.IsNullOrEmpty(envVarValue))
                    {
                        configuration[kvp.Key] = envVarValue;
                    }
                }
            }
        }

        public static void ConfigureCors(IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
                ?? throw new InvalidOperationException("The CORS allowed origins are not configured in the application settings.");

            services.AddCors(options =>
            {
                options.AddPolicy("DefaultCorsPolicy", builder =>
                {
                    builder.WithOrigins(allowedOrigins)
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials()
                           .WithHeaders(
                               HeaderNames.ContentType,
                               HeaderNames.Server,
                               HeaderNames.AccessControlAllowHeaders,
                               HeaderNames.AccessControlExposeHeaders,
                               "x-custom-header",
                               "x-path",
                               "x-record-in-use",
                               HeaderNames.ContentDisposition
                           );
                });
            });
        }

        public static void ConfigureLogging(WebApplicationBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File($"{builder.Configuration.GetValue<string>("Logging:LogFilePath")}log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            builder.Host.UseSerilog();
        }

        public static void ConfigureJwtAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            var key = configuration.GetValue<string>("JwtSettings:Key")
                ?? throw new InvalidOperationException("JWT Key is not configured.");
            var keyBytes = Encoding.UTF8.GetBytes(key);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration.GetValue<string>("JwtSettings:Issuer"),
                    ValidAudience = configuration.GetValue<string>("JwtSettings:Audience"),
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
                };
            });

            services.AddAuthorization();
        }

        public static void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ACCESS CONTROL API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer'[space] followed by your token in the field below.\r\n\r\nExample: 'Bearer 12345abcdef'",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });
            });

            services.AddSwaggerGenNewtonsoftSupport();
            services.AddEndpointsApiExplorer();
        }
    }

}
