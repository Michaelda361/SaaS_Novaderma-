using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
using TalentManagement.Infrastructure.Persistence;
using TalentManagement.Shared.DTOs.Colaboradores;
using Xunit;

namespace TalentManagement.IntegrationTests;

public class ColaboradorCampoTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ColaboradorCampoTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeleteCampo_AsAdmin_Success()
    {
        // 1. Arrange: Create admin user and a custom field definition in DB
        var email = "admin-campo-test@novaderma.com";
        int campoId;
        
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
                    Apellido = "Campo",
                    Email = email,
                    Rol = RolUsuario.Admin,
                    AreaId = area.Id,
                    CargoId = cargo.Id,
                    FechaIngreso = DateTime.UtcNow
                };
                db.Colaboradores.Add(admin);
                await db.SaveChangesAsync();
            }

            var campo = new ColaboradorCampoDefinicion
            {
                CampoClave = "campo_test_hobbies",
                Nombre = "Hobbies",
                Tipo = ColaboradorCampoTipo.Texto,
                Requerido = false,
                Orden = 99,
                Activo = true
            };
            db.ColaboradorCampoDefiniciones.Add(campo);
            await db.SaveChangesAsync();
            campoId = campo.Id;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", email);

        // 2. Act: Call DELETE api/v1/colaboradores/campos/{id}
        var response = await client.DeleteAsync($"/api/v1/colaboradores/campos/{campoId}");

        // 3. Assert status code is 204 NoContent
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 4. Verify that GET api/v1/colaboradores/campos does NOT return it
        var getResponse = await client.GetAsync("/api/v1/colaboradores/campos");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var list = await getResponse.Content.ReadFromJsonAsync<List<ColaboradorCampoDto>>();
        list.Should().NotBeNull();
        list!.Any(x => x.Id == campoId).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCampo_AsColaborador_ReturnsForbidden()
    {
        // 1. Arrange: Create a non-admin user and a custom field definition in DB
        var email = "colab-campo-test@novaderma.com";
        int campoId;
        
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
                    Apellido = "Campo",
                    Email = email,
                    Rol = RolUsuario.Colaborador,
                    AreaId = area.Id,
                    CargoId = cargo.Id,
                    FechaIngreso = DateTime.UtcNow
                };
                db.Colaboradores.Add(colab);
                await db.SaveChangesAsync();
            }

            var campo = new ColaboradorCampoDefinicion
            {
                CampoClave = "campo_test_deporte",
                Nombre = "Deporte",
                Tipo = ColaboradorCampoTipo.Texto,
                Requerido = false,
                Orden = 100,
                Activo = true
            };
            db.ColaboradorCampoDefiniciones.Add(campo);
            await db.SaveChangesAsync();
            campoId = campo.Id;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", email);

        // 2. Act: Call DELETE api/v1/colaboradores/campos/{id}
        var response = await client.DeleteAsync($"/api/v1/colaboradores/campos/{campoId}");

        // 3. Assert status code is 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteCampo_WithAssociatedValues_SuccessAndSoftDeleted()
    {
        // 1. Arrange: Create admin user, a custom field, a collaborator, and assign a value to the custom field.
        var email = "admin-val-test@novaderma.com";
        int campoId;
        int colaboradorId;

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
                    Apellido = "Valor",
                    Email = email,
                    Rol = RolUsuario.Admin,
                    AreaId = area.Id,
                    CargoId = cargo.Id,
                    FechaIngreso = DateTime.UtcNow
                };
                db.Colaboradores.Add(admin);
                await db.SaveChangesAsync();
            }

            var campo = new ColaboradorCampoDefinicion
            {
                CampoClave = "campo_test_hobbies_val",
                Nombre = "Hobbies Val",
                Tipo = ColaboradorCampoTipo.Texto,
                Requerido = false,
                Orden = 99,
                Activo = true
            };
            db.ColaboradorCampoDefiniciones.Add(campo);
            await db.SaveChangesAsync();
            campoId = campo.Id;

            var colaborador = new Colaborador
            {
                Nombre = "Juan",
                Apellido = "Pérez",
                Email = "juan.perez.val@novaderma.com",
                Rol = RolUsuario.Colaborador,
                AreaId = area.Id,
                CargoId = cargo.Id,
                FechaIngreso = DateTime.UtcNow
            };
            db.Colaboradores.Add(colaborador);
            await db.SaveChangesAsync();
            colaboradorId = colaborador.Id;

            var valor = new ColaboradorCampoValor
            {
                ColaboradorId = colaboradorId,
                ColaboradorCampoDefinicionId = campoId,
                Valor = "Fútbol",
                Activo = true
            };
            db.ColaboradorCampoValores.Add(valor);
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", email);

        // 2. Act: Call DELETE api/v1/colaboradores/campos/{id}
        var response = await client.DeleteAsync($"/api/v1/colaboradores/campos/{campoId}");

        // 3. Assert status code is 204 NoContent (should successfully soft-delete despite existing values)
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 4. Verify that GET api/v1/colaboradores/campos does NOT return the deleted field
        var getResponse = await client.GetAsync("/api/v1/colaboradores/campos");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var list = await getResponse.Content.ReadFromJsonAsync<List<ColaboradorCampoDto>>();
        list.Should().NotBeNull();
        list!.Any(x => x.Id == campoId).Should().BeFalse();

        // 5. Verify that GET api/v1/colaboradores/{id} does not throw an exception and loads successfully
        var getColabResponse = await client.GetAsync($"/api/v1/colaboradores/{colaboradorId}");
        getColabResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var colabDto = await getColabResponse.Content.ReadFromJsonAsync<ColaboradorDto>();
        colabDto.Should().NotBeNull();
        colabDto!.CamposAdicionales.Should().NotContainKey("campo_test_hobbies_val");
    }



    [Fact]
    public async Task CreateUpdateDeleteColaborador_AsColaborador_ReturnsForbidden()
    {
        // 1. Arrange: Create a non-privileged user (Colaborador)
        var email = "normal-user@novaderma.com";
        int targetColabId;
        int areaId;
        int cargoId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var area = await db.Areas.FirstAsync();
            var cargo = await db.Cargos.FirstAsync();
            areaId = area.Id;
            cargoId = cargo.Id;

            var colab = new Colaborador
            {
                Nombre = "User",
                Apellido = "Normal",
                Email = email,
                Rol = RolUsuario.Colaborador,
                AreaId = areaId,
                CargoId = cargoId,
                FechaIngreso = DateTime.UtcNow
            };
            db.Colaboradores.Add(colab);

            var target = new Colaborador
            {
                Nombre = "Target",
                Apellido = "Colab",
                Email = "target.colab@novaderma.com",
                Rol = RolUsuario.Colaborador,
                AreaId = areaId,
                CargoId = cargoId,
                FechaIngreso = DateTime.UtcNow
            };
            db.Colaboradores.Add(target);
            await db.SaveChangesAsync();
            targetColabId = target.Id;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", email);

        // 2. Act & Assert: Create should return Forbidden
        var createDto = new CreateColaboradorDto
        {
            Nombre = "New",
            Apellido = "Colab",
            Email = "new.colab@novaderma.com",
            AreaId = areaId,
            CargoId = cargoId,
            FechaIngreso = DateTime.UtcNow
        };
        var createResponse = await client.PostAsJsonAsync("/api/v1/colaboradores", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. Act & Assert: Update should return Forbidden
        var updateDto = new UpdateColaboradorDto
        {
            Nombre = "Updated Target",
            Apellido = "Colab",
            Email = "target.colab@novaderma.com",
            AreaId = areaId,
            CargoId = cargoId
        };
        var updateResponse = await client.PutAsJsonAsync($"/api/v1/colaboradores/{targetColabId}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 4. Act & Assert: Delete should return Forbidden
        var deleteResponse = await client.DeleteAsync($"/api/v1/colaboradores/{targetColabId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 5. Act & Assert: CambiarRol should return Forbidden
        var rolDto = new CambiarRolDto { Rol = "Admin" };
        var rolResponse = await client.PutAsJsonAsync($"/api/v1/colaboradores/{targetColabId}/rol", rolDto);
        rolResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
