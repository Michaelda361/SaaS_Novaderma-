using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net.Http;
using TalentManagement.Infrastructure.Persistence;

namespace TalentManagement.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TalentManagementDB_Tests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Usar Development para asegurar que se usen servicios Mock de SharePoint/Storage si no hay configuraciones reales
        builder.UseEnvironment("Development");

        // Forzar el uso de EnsureCreated en lugar de Migrate para evitar problemas con migraciones historicas inconsistentes en pruebas de integracion
        builder.UseSetting("DatabaseSettings:UseEnsureCreated", "true");

        builder.ConfigureServices(services =>
        {
            // 1. Remover DbContextOptions registrado por defecto
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 2. Registrar DbContextOptions usando la cadena de conexión con base de datos única para esta instancia
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(
                    $"Server=localhost;Database={_dbName};Trusted_Connection=True;TrustServerCertificate=True;",
                    sqlOptions => sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null));
            });

            // 3. Registrar el esquema de autenticación TestAuth
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, _ => { });

            // 4. Registrar la política de autorización por defecto para usar TestAuth
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(TestAuthHandler.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                using var scope = Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                // Limpiar la base de datos temporal al finalizar las pruebas de esta factoría
                db.Database.EnsureDeleted();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar la base de datos de prueba {_dbName}: {ex.Message}");
            }
        }
        base.Dispose(disposing);
    }
}
