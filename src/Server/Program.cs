using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Scalar.AspNetCore;
using TalentManagement.Infrastructure;
using TalentManagement.Server;
using TalentManagement.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"[NovaHub] Environment: {builder.Environment.EnvironmentName}");

// Inyectar ContentRootPath en config para que MockSharePointService lo pueda leer
builder.Configuration["ContentRootPath"] = builder.Environment.ContentRootPath;

// Producción / Despliegue: solo JWT, multi-tenant
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);
builder.Services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
    Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        var clientId = builder.Configuration["AzureAd:ClientId"];
        var audience = builder.Configuration["AzureAd:Audience"] ?? $"api://{clientId}";
        options.TokenValidationParameters.ValidAudiences = new[] { clientId, audience };
    });
builder.Services.AddAuthorization();

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
    db.Database.Migrate();
    await TalentManagement.Infrastructure.Persistence.DbSeeder.SeedAsync(db);
    await TalentManagement.Infrastructure.Persistence.DbSeeder.CorregirHistoricosHuerfanosAsync(db);
}

app.UseMiddleware<TalentManagement.Server.ExceptionHandlingMiddleware>();

app.UseCors();
app.UseResponseCompression();
app.UseStaticFiles();

app.MapRazorPages();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificacionesHub>("/hubs/notificaciones");

// Endpoint para que el cliente sepa si el usuario actual es Microsoft (Bearer) o dev
app.MapGet("/api/v1/auth/es-microsoft", () => Results.Ok(new { esMicrosoft = true })).RequireAuthorization();

// Perfil del usuario actual: rol dentro de la app
app.MapGet("/api/v1/auth/perfil", async (
    HttpContext ctx,
    TalentManagement.Server.Services.CurrentUserService currentUser,
    TalentManagement.Application.Interfaces.IColaboradorRepository colaboradorRepo) =>
{
    try
    {
        var esMicrosoft = currentUser.EsMicrosoftUser();
        var email = currentUser.GetEmail();
        var colaborador = await colaboradorRepo.GetByEmailAsync(email);
        var esJefe = colaborador is not null &&
                     await colaboradorRepo.EsJefeDeAreaAsync(colaborador.Id);
        var puedeResolver = await currentUser.PuedeResolverSolicitudesAsync();

        // Nombre: usar el de la BD si existe, si no el del token
        var nombreMostrar = colaborador is not null
            ? $"{colaborador.Nombre} {colaborador.Apellido}"
            : ctx.User.FindFirst("name")?.Value
              ?? ctx.User.FindFirst("preferred_username")?.Value
              ?? email;

        return Results.Ok(new
        {
            email,
            nombre = nombreMostrar,
            esColaborador = colaborador is not null,
            esJefe,
            colaboradorId = colaborador?.Id,
            areaId = colaborador?.AreaId,
            esDevUser = !esMicrosoft,
            // Si no tiene colaborador en BD: mínimo privilegio (Colaborador), no Admin
            rol = colaborador?.Rol.ToString() ?? "Colaborador",
            puedeResolverSolicitudes = puedeResolver,
        });
    }
    catch
    {
        return Results.Ok(new { email = "", nombre = "Usuario", esColaborador = false, esJefe = false, colaboradorId = (int?)null, esDevUser = false, rol = "Colaborador", puedeResolverSolicitudes = false });
    }
}).RequireAuthorization();

app.MapFallbackToFile("index.html");

app.Run();
