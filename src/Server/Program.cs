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

// Detectar modo dev: por environment O por presencia de DevSettings en config
var esModoDev = builder.Environment.IsDevelopment()
    || !string.IsNullOrWhiteSpace(builder.Configuration["DevSettings:DefaultDevUser"]);

Console.WriteLine($"[NovaHub] Environment: {builder.Environment.EnvironmentName} | DevMode: {esModoDev}");

if (esModoDev)
{
    // Dev: acepta JWT de Entra ID Y el esquema DevUser (header X-Dev-User)
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

    builder.Services.AddAuthentication()
        .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>("DevUser", _ => { });
    builder.Services.AddAuthorization(options =>
        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes("Bearer", "DevUser")
            .RequireAuthenticatedUser()
            .Build());

    builder.Services.AddSingleton<TalentManagement.Server.Services.DevUserStore>();
}
else
{
    // Producción: solo JWT, multi-tenant
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);
}

if (esModoDev)
{
    // En desarrollo: relaja la validación de JWT para facilitar pruebas con distintos tenants
    builder.Services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
        Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
        options =>
        {
            options.TokenValidationParameters.ValidateIssuer = false;
            options.TokenValidationParameters.ValidateAudience = false;
        });
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TalentManagement.Server.Services.CurrentUserService>();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5185", "https://localhost:5185",
                           "http://localhost:5000", "https://localhost:5001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()));

var app = builder.Build();

if (esModoDev)
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (esModoDev)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider
        .GetRequiredService<TalentManagement.Infrastructure.Persistence.AppDbContext>();
    db.Database.Migrate();
    await TalentManagement.Infrastructure.Persistence.DbSeeder.SeedAsync(db);
    await TalentManagement.Infrastructure.Persistence.DbSeeder.CorregirHistoricosHuerfanosAsync(db);
    await TalentManagement.Infrastructure.Persistence.DbSeeder.RunIntegrationTestsAsync(scope.ServiceProvider);
}

app.UseMiddleware<TalentManagement.Server.ExceptionHandlingMiddleware>();

app.UseCors();
app.UseResponseCompression();
app.UseStaticFiles();

if (esModoDev)
    app.MapRazorPages();

if (esModoDev)
{
    app.MapGet("/api/v1/dev/usuario-activo", (TalentManagement.Server.Services.DevUserStore store) =>
        Results.Ok(new { email = store.ActiveEmail, activo = store.ActiveEmail != null }))
        .AllowAnonymous();

    // Diagnóstico del hub: muestra qué colaboradores están conectados
    app.MapGet("/api/v1/dev/hub-conexiones", () =>
        Results.Ok(TalentManagement.Server.Hubs.NotificacionesHub.GetConexionesActivas()))
        .AllowAnonymous();

    // Test: envía una notificación de prueba directamente a un colaborador
    app.MapPost("/api/v1/dev/hub-test/{colaboradorId:int}", async (
        int colaboradorId,
        IHubContext<TalentManagement.Server.Hubs.NotificacionesHub> hub) =>
    {
        var connId = TalentManagement.Server.Hubs.NotificacionesHub.GetConnectionId(colaboradorId);
        if (connId is null)
            return Results.NotFound(new { error = $"ColaboradorId={colaboradorId} no está conectado" });

        var testNotif = new TalentManagement.Shared.DTOs.Inscripciones.InscripcionDto
        {
            Id = 9999,
            ColaboradorId = colaboradorId,
            ColaboradorNombre = "Test",
            ColaboradorEmail = "test@test.com",
            CapacitacionId = 9999,
            CapacitacionNombre = "🧪 Notificación de prueba",
            FechaInscripcion = DateTime.Now,
        };

        await hub.Clients.Client(connId).SendAsync("InscripcionCreada", testNotif);
        return Results.Ok(new { enviado = true, connectionId = connId, colaboradorId });
    }).AllowAnonymous();

    app.MapPost("/api/v1/dev/usuario-activo", (
        TalentManagement.Server.Services.DevUserStore store,
        DevUsuarioRequest req) =>
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            store.ClearUser();
        else
            store.SetUser(req.Email);
        return Results.Ok(new { email = store.ActiveEmail, activo = store.ActiveEmail != null });
    }).AllowAnonymous();
}


if (!esModoDev)
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificacionesHub>("/hubs/notificaciones");

// Endpoint para que el cliente sepa si el usuario actual es Microsoft (Bearer) o dev
app.MapGet("/api/v1/auth/es-microsoft", (HttpContext ctx) =>
{
    var esMicrosoft = !ctx.User.Identities.Any(i => i.AuthenticationType == "DevUser");
    return Results.Ok(new { esMicrosoft });
}).RequireAuthorization();

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

app.Run();

record DevUsuarioRequest(string? Email);
