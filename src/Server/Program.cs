using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Scalar.AspNetCore;
using Serilog;
using System.Threading.RateLimiting;
using TalentManagement.Infrastructure;
using TalentManagement.Server;
using TalentManagement.Server.Hubs;


var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

Console.WriteLine($"[NovaHub] Environment: {builder.Environment.EnvironmentName}");

// Inyectar ContentRootPath en config para que MockSharePointService lo pueda leer
builder.Configuration["ContentRootPath"] = builder.Environment.ContentRootPath;

// Cargar configuración externa opcional (fuera del directorio de publicación)
var externalConfigPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "TalentManagement",
    "appsettings.Production.json");
if (File.Exists(externalConfigPath))
{
    builder.Configuration.AddJsonFile(externalConfigPath, optional: true, reloadOnChange: true);
}

// Producción / Despliegue: solo JWT, multi-tenant
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);
builder.Services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
    Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        var clientId = builder.Configuration["AzureAd:ClientId"];
        var audience = builder.Configuration["AzureAd:Audience"] ?? $"api://{clientId}";
        options.TokenValidationParameters.ValidAudiences = new[] { clientId, audience };
        
        var validateIssuerStr = builder.Configuration["AzureAd:ValidateIssuer"];
        if (bool.TryParse(validateIssuerStr, out var validateIssuer))
        {
            options.TokenValidationParameters.ValidateIssuer = validateIssuer;
        }

        // Permitir autenticación SignalR usando token en Query String
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notificaciones"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication()
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevAuthHandler>("DevUser", _ => { });
    builder.Services.AddAuthorization(options =>
        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes("Bearer", "DevUser")
            .RequireAuthenticatedUser()
            .Build());
    builder.Services.AddSingleton<TalentManagement.Server.Services.DevUserStore>();
}
else
{
    builder.Services.AddAuthorization();
}

builder.Services.AddOpenApi();
builder.Services.AddRazorPages();
builder.Services.AddSignalR(options =>
{
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});
builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = true;
    opts.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes
        .Concat(["application/json", "application/pdf"]);
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TalentManagement.Infrastructure.Persistence.AppDbContext>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TalentManagement.Server.Services.CurrentUserService>();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
    {
        var originsStr = builder.Configuration["CorsOrigins"];
        var origins = !string.IsNullOrEmpty(originsStr)
            ? originsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : new[] { "http://localhost:5185", "https://localhost:5185", "http://localhost:5000", "https://localhost:5001" };
        policy.WithOrigins(origins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    }));

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("upload", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirst("preferred_username")?.Value 
                          ?? httpContext.Connection.RemoteIpAddress?.ToString() 
                          ?? "global",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<TalentManagement.Infrastructure.Persistence.AppDbContext>();
    
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    if (config.GetValue<bool>("DatabaseSettings:UseEnsureCreated"))
    {
        db.Database.EnsureCreated();
    }
    else
    {
        db.Database.Migrate();
    }

    await TalentManagement.Infrastructure.Persistence.DbSeeder.SeedAsync(db);
    await TalentManagement.Infrastructure.Persistence.DbSeeder.CorregirHistoricosHuerfanosAsync(db);
}

app.UseMiddleware<TalentManagement.Server.ExceptionHandlingMiddleware>();

app.UseCors();
app.UseRateLimiter();
app.UseResponseCompression();
app.UseStaticFiles();

app.MapRazorPages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificacionesHub>("/hubs/notificaciones");
app.MapHealthChecks("/health");



app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }

