using AccessControl.Application.Mappings;
using AccessControl.Application.Middleware;
using AccessControl.Application.Services.BackgroundTaskService;
using AccessControl.Application.Services.EmailService;
using AccessControl.Application.Services.LogService;
using AccessControl.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

ConfigurationHelper.ReplaceEnvironmentVariables(builder.Configuration);
ConfigurationHelper.ConfigureLogging(builder);
ConfigurationHelper.ConfigureCors(builder.Services, builder.Configuration);

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
    {
        NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy(),
        IgnoreSerializableAttribute = true
    };
    options.SerializerSettings.DateFormatString = "dd/MM/yyyy HH:mm:ss";
});

ConfigurationHelper.ConfigureIdentityAndDbContext(builder.Services, builder.Configuration);
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddAutoMapper(typeof(MappingProfile));

ConfigurationHelper.ConfigureSwagger(builder.Services);
ConfigurationHelper.ConfigureJwtAuthentication(builder.Services, builder.Configuration);

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<BackgroundTaskService>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("DefaultCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
