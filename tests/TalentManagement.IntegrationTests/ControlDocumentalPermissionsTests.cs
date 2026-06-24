using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
using TalentManagement.Infrastructure.Persistence;
using TalentManagement.Shared.DTOs.ControlDocumental;
using Xunit;

namespace TalentManagement.IntegrationTests;

public class ControlDocumentalPermissionsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ControlDocumentalPermissionsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateListadoMaestro_AsAdmin_Success()
    {
        // Arrange
        var email = "admin-cd-test@novaderma.com";
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
                    Apellido = "CD",
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

        var dto = new CreateListadoMaestroDto
        {
            Nombre = "Listado de Pruebas Admin",
            Descripcion = "Pruebas de creación",
            Campos = new List<DocumentoControlCampoDto>
            {
                new() { CampoClave = "procedimiento", Nombre = "Procedimiento Relacionado", Tipo = "Texto", Orden = 1 }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/control-documental/listados-maestros", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ListadoMaestroDto>();
        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Listado de Pruebas Admin");
    }

    [Fact]
    public async Task CreateListadoMaestro_AsColaborador_ReturnsForbidden()
    {
        // Arrange
        var email = "colab-cd-test@novaderma.com";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var area = await db.Areas.FirstAsync();
            var cargo = await db.Cargos.FirstAsync();

            var colab = await db.Colaboradores.FirstOrDefaultAsync(c => c.Email == email);
            if (colab == null)
            {
                colab = new Colaborador
                {
                    Nombre = "Colaborador",
                    Apellido = "CD",
                    Email = email,
                    Rol = RolUsuario.Colaborador,
                    AreaId = area.Id,
                    CargoId = cargo.Id,
                    FechaIngreso = DateTime.UtcNow
                };
                db.Colaboradores.Add(colab);
                await db.SaveChangesAsync();
            }
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", email);

        var dto = new CreateListadoMaestroDto
        {
            Nombre = "Listado de Pruebas Colaborador",
            Descripcion = "Pruebas de creación"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/control-documental/listados-maestros", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetListadoMaestro_AsColaboradorWithoutPermissions_ReturnsForbidden()
    {
        // Arrange
        var colabEmail = "colab-no-perm@novaderma.com";
        int listadoId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var area = await db.Areas.FirstAsync();
            var cargo = await db.Cargos.FirstAsync();

            // Asegurar colaborador sin permisos
            var colab = await db.Colaboradores.FirstOrDefaultAsync(c => c.Email == colabEmail);
            if (colab == null)
            {
                colab = new Colaborador
                {
                    Nombre = "Colaborador",
                    Apellido = "Sin Permisos",
                    Email = colabEmail,
                    Rol = RolUsuario.Colaborador,
                    AreaId = area.Id,
                    CargoId = cargo.Id,
                    FechaIngreso = DateTime.UtcNow
                };
                db.Colaboradores.Add(colab);
            }

            // Crear un listado maestro
            var listado = new ListadoMaestro
            {
                Nombre = "Listado Restringido",
                Descripcion = "Solo para algunos"
            };
            db.ListadosMaestros.Add(listado);
            await db.SaveChangesAsync();
            listadoId = listado.Id;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", colabEmail);

        // Act
        var response = await client.GetAsync($"/api/v1/control-documental/listados-maestros/{listadoId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetListadoMaestro_AsColaboradorWithExplicitPermission_ReturnsOk()
    {
        // Arrange
        var colabEmail = "colab-with-perm@novaderma.com";
        int listadoId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var area = await db.Areas.FirstAsync();
            var cargo = await db.Cargos.FirstAsync();

            // Asegurar colaborador
            var colab = await db.Colaboradores.FirstOrDefaultAsync(c => c.Email == colabEmail);
            if (colab == null)
            {
                colab = new Colaborador
                {
                    Nombre = "Colaborador",
                    Apellido = "Con Permisos",
                    Email = colabEmail,
                    Rol = RolUsuario.Colaborador,
                    AreaId = area.Id,
                    CargoId = cargo.Id,
                    FechaIngreso = DateTime.UtcNow
                };
                db.Colaboradores.Add(colab);
                await db.SaveChangesAsync();
            }

            // Crear un listado maestro
            var listado = new ListadoMaestro
            {
                Nombre = "Listado Accesible",
                Descripcion = "Con permisos explicitos"
            };
            db.ListadosMaestros.Add(listado);
            await db.SaveChangesAsync();
            listadoId = listado.Id;

            // Asignar permiso explícito de Ver
            var permiso = new ListadoMaestroPermiso
            {
                ListadoMaestroId = listadoId,
                ColaboradorId = colab.Id,
                PuedeVer = true
            };
            db.ListadoMaestroPermisos.Add(permiso);
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", colabEmail);

        // Act
        var response = await client.GetAsync($"/api/v1/control-documental/listados-maestros/{listadoId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ListadoMaestroDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(listadoId);
        result.Nombre.Should().Be("Listado Accesible");
    }
}
