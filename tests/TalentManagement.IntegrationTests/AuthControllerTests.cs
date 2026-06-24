using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
using TalentManagement.Infrastructure.Persistence;
using Xunit;

namespace TalentManagement.IntegrationTests;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPerfil_AnonymousUser_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/auth/perfil");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPerfil_NotRegisteredUser_ReturnsBasicColaboradorProfile()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", "not-registered@novaderma.com");

        // Act
        var response = await client.GetAsync("/api/v1/auth/perfil");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var profile = await response.Content.ReadFromJsonAsync<TestProfileResponse>();
        profile.Should().NotBeNull();
        profile!.Rol.Should().Be("Colaborador");
        profile.EsColaborador.Should().BeFalse();
        profile.EsJefe.Should().BeFalse();
        profile.PuedeResolverSolicitudes.Should().BeFalse();
    }

    [Fact]
    public async Task GetPerfil_AdminUser_ReturnsAdminProfile()
    {
        // Arrange
        var email = "admin-test@novaderma.com";
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var area = await db.Areas.FirstAsync();
            var cargo = await db.Cargos.FirstAsync();

            var admin = await db.Colaboradores.FirstOrDefaultAsync(c => c.Email == email);
            if (admin == null)
            {
                admin = new Colaborador
                {
                    Nombre = "Admin",
                    Apellido = "Test",
                    Email = email,
                    Rol = RolUsuario.Admin,
                    AreaId = area.Id,
                    CargoId = cargo.Id,
                    FechaIngreso = DateTime.UtcNow
                };
                db.Colaboradores.Add(admin);
                await db.SaveChangesAsync();
            }
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", email);

        // Act
        var response = await client.GetAsync("/api/v1/auth/perfil");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<TestProfileResponse>();
        profile.Should().NotBeNull();
        profile!.Rol.Should().Be("Admin");
        profile.EsColaborador.Should().BeTrue();
        profile.EsJefe.Should().BeFalse();
        profile.PuedeResolverSolicitudes.Should().BeTrue();
    }

    [Fact]
    public async Task GetPerfil_JefeUser_ReturnsJefeProfile()
    {
        // Arrange
        var email = "jefe-test@novaderma.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var area = await db.Areas.FirstAsync();
            var cargo = await db.Cargos.FirstAsync();

            var jefe = await db.Colaboradores.FirstOrDefaultAsync(c => c.Email == email);
            if (jefe == null)
            {
                jefe = new Colaborador
                {
                    Nombre = "Jefe",
                    Apellido = "Test",
                    Email = email,
                    Rol = RolUsuario.Jefe,
                    AreaId = area.Id,
                    CargoId = cargo.Id,
                    FechaIngreso = DateTime.UtcNow
                };
                db.Colaboradores.Add(jefe);
                await db.SaveChangesAsync();

                // Establecer al jefe de area en el area correspondiente
                area.JefeId = jefe.Id;
                db.Entry(area).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", email);

        // Act
        var response = await client.GetAsync("/api/v1/auth/perfil");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<TestProfileResponse>();
        profile.Should().NotBeNull();
        profile!.Rol.Should().Be("Jefe");
        profile.EsColaborador.Should().BeTrue();
        profile.EsJefe.Should().BeTrue();
        profile.PuedeResolverSolicitudes.Should().BeTrue();
    }
}

public class TestProfileResponse
{
    public string Email { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool EsColaborador { get; set; }
    public bool EsJefe { get; set; }
    public int? ColaboradorId { get; set; }
    public int? AreaId { get; set; }
    public bool EsDevUser { get; set; }
    public string Rol { get; set; } = string.Empty;
    public bool PuedeResolverSolicitudes { get; set; }
}
