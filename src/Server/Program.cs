using Microsoft.AspNetCore.Authentication;
using Microsoft.Identity.Web;
using Scalar.AspNetCore;
using TalentManagement.Infrastructure;
using TalentManagement.Server;

var builder = WebApplication.CreateBuilder(args);

// Inyectar ContentRootPath en config para que MockSharePointService lo pueda leer
builder.Configuration["ContentRootPath"] = builder.Environment.ContentRootPath;

if (builder.Environment.IsDevelopment())
{
    // En dev: acepta tanto JWT de Entra ID como el esquema DevUser (header X-Dev-User)
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);
    builder.Services.AddAuthentication()
        .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>("DevUser", _ => { });
    builder.Services.AddAuthorization(options =>
        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes("Bearer", "DevUser")
            .RequireAuthenticatedUser()
            .Build());

    // Singleton para cambiar el usuario dev en caliente
    builder.Services.AddSingleton<TalentManagement.Server.Services.DevUserStore>();
}
else
{
    // Producción: solo JWT
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);
}

builder.Services.AddOpenApi();
builder.Services.AddRazorPages(); // para la página /dev
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TalentManagement.Server.Services.CurrentUserService>();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5185", "https://localhost:5185",
                           "http://localhost:5000", "https://localhost:5001")
              .AllowAnyMethod()
              .AllowAnyHeader()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider
        .GetRequiredService<TalentManagement.Infrastructure.Persistence.AppDbContext>();
    await TalentManagement.Infrastructure.Persistence.DbSeeder.SeedAsync(db);
}

app.UseCors();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
    app.MapRazorPages(); // sirve /dev

// Endpoints dev — gestión de usuario activo desde el cliente Blazor
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/v1/dev/usuario-activo", (TalentManagement.Server.Services.DevUserStore store) =>
        Results.Ok(new { email = store.ActiveEmail, activo = store.ActiveEmail != null }))
        .AllowAnonymous();

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

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

record DevUsuarioRequest(string? Email);
