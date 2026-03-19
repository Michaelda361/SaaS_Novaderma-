using Microsoft.Identity.Web;
using Scalar.AspNetCore;
using TalentManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Auth — valida tokens JWT emitidos por Entra ID
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);

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
    var db = scope.ServiceProvider.GetRequiredService<TalentManagement.Infrastructure.Persistence.AppDbContext>();
    await TalentManagement.Infrastructure.Persistence.DbSeeder.SeedAsync(db);
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
