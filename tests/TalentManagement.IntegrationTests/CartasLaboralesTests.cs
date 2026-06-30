using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
using TalentManagement.Infrastructure.Persistence;
using TalentManagement.Shared.DTOs.PlantillasDocumento;
using Xunit;

namespace TalentManagement.IntegrationTests;

public class CartasLaboralesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CartasLaboralesTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // ── Bug #6 — Soft-delete excluye plantillas del listado ───────────────────

    [Fact]
    public async Task GetDisponibles_ShouldNotReturn_SoftDeletedPlantillas()
    {
        var colaboradorEmail = "colab-cartas-1@novaderma.com";

        int plantillaActivaId;
        int plantillaEliminadaId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var area = await db.Areas.FirstAsync();
            var cargo = await db.Cargos.FirstAsync();

            // Crear colaborador de prueba
            var colab = await db.Colaboradores.FirstOrDefaultAsync(c => c.Email == colaboradorEmail);
            if (colab == null)
            {
                colab = new Colaborador
                {
                    Nombre = "Colab",
                    Apellido = "CartasTest",
                    Email = colaboradorEmail,
                    Rol = RolUsuario.Colaborador,
                    AreaId = area.Id,
                    CargoId = cargo.Id,
                    FechaIngreso = DateTime.UtcNow
                };
                db.Colaboradores.Add(colab);
                await db.SaveChangesAsync();
            }

            // Plantilla activa
            var plantillaActiva = new PlantillaDocumento
            {
                Nombre = "Carta de Empleo Activa",
                TipoPlantilla = TipoPlantilla.Html,
                ContenidoHtml = "<p>Hola {{nombre_completo}}</p>",
                AplicaTodasAreas = true,
                Activo = true
            };
            db.PlantillasDocumento.Add(plantillaActiva);

            // Plantilla eliminada (soft-delete)
            var plantillaEliminada = new PlantillaDocumento
            {
                Nombre = "Carta de Empleo Eliminada",
                TipoPlantilla = TipoPlantilla.Html,
                ContenidoHtml = "<p>Eliminada</p>",
                AplicaTodasAreas = true,
                Activo = false // soft-deleted
            };
            db.PlantillasDocumento.Add(plantillaEliminada);
            await db.SaveChangesAsync();

            plantillaActivaId = plantillaActiva.Id;
            plantillaEliminadaId = plantillaEliminada.Id;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", colaboradorEmail);

        var response = await client.GetAsync("/api/v1/plantillasdocumento/disponibles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await response.Content.ReadFromJsonAsync<List<PlantillaDocumentoDto>>();
        list.Should().NotBeNull();

        // La plantilla activa debe aparecer
        list!.Any(p => p.Id == plantillaActivaId).Should().BeTrue(
            "una plantilla activa debe ser visible para el colaborador");

        // La plantilla eliminada NO debe aparecer (Bug #6 fix)
        list.Any(p => p.Id == plantillaEliminadaId).Should().BeFalse(
            "una plantilla con Activo=false (soft-delete) no debe aparecer en listados");
    }

    // ── Detección de solicitud pendiente duplicada ────────────────────────────

    [Fact]
    public async Task EnviarSolicitud_WhenPendingExists_ShouldReturnConflict()
    {
        var colaboradorEmail = "colab-cartas-2@novaderma.com";

        int plantillaId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var area = await db.Areas.FirstAsync();
            var cargo = await db.Cargos.FirstAsync();

            var colab = await db.Colaboradores.FirstOrDefaultAsync(c => c.Email == colaboradorEmail);
            if (colab == null)
            {
                colab = new Colaborador
                {
                    Nombre = "Colab",
                    Apellido = "Duplicado",
                    Email = colaboradorEmail,
                    Rol = RolUsuario.Colaborador,
                    AreaId = area.Id,
                    CargoId = cargo.Id,
                    FechaIngreso = DateTime.UtcNow
                };
                db.Colaboradores.Add(colab);
                await db.SaveChangesAsync();
            }

            var plantilla = new PlantillaDocumento
            {
                Nombre = "Carta Duplicado Test",
                TipoPlantilla = TipoPlantilla.Html,
                ContenidoHtml = "<p>Test</p>",
                AplicaTodasAreas = true,
                Activo = true
            };
            db.PlantillasDocumento.Add(plantilla);
            await db.SaveChangesAsync();
            plantillaId = plantilla.Id;

            // Solicitud pendiente ya existente
            var solicitudExistente = new SolicitudDocumento
            {
                PlantillaDocumentoId = plantillaId,
                ColaboradorId = colab.Id,
                FechaSolicitud = DateTime.UtcNow,
                Estado = EstadoSolicitud.Pendiente,
                PdfFileKey = "solicitudes-pdf/test_existente.pdf",
                NotificadoColaborador = true
            };
            db.SolicitudesDocumento.Add(solicitudExistente);
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", colaboradorEmail);

        // Intentar crear una segunda solicitud para la misma plantilla -> debe retornar 409 Conflict
        var dto = new EnviarSolicitudDto { Extras = [] };
        var response = await client.PostAsJsonAsync($"/api/v1/plantillasdocumento/{plantillaId}/solicitar", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "no se debe permitir crear una segunda solicitud pendiente para la misma plantilla");
    }

    // ── Listado admin no retorna plantillas soft-deleted ──────────────────────

    [Fact]
    public async Task GetAll_Admin_ShouldNotReturn_SoftDeletedPlantillas()
    {
        var adminEmail = "admin-cartas-3@novaderma.com";
        int plantillaEliminadaId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var area = await db.Areas.FirstAsync();
            var cargo = await db.Cargos.FirstAsync();

            var admin = await db.Colaboradores.FirstOrDefaultAsync(c => c.Email == adminEmail);
            if (admin == null)
            {
                admin = new Colaborador
                {
                    Nombre = "Admin",
                    Apellido = "CartasAdmin",
                    Email = adminEmail,
                    Rol = RolUsuario.Admin,
                    AreaId = area.Id,
                    CargoId = cargo.Id,
                    FechaIngreso = DateTime.UtcNow
                };
                db.Colaboradores.Add(admin);
                await db.SaveChangesAsync();
            }

            var plantillaEliminada = new PlantillaDocumento
            {
                Nombre = "Carta Eliminada Admin Test",
                TipoPlantilla = TipoPlantilla.Html,
                ContenidoHtml = "<p>Eliminada</p>",
                AplicaTodasAreas = true,
                Activo = false
            };
            db.PlantillasDocumento.Add(plantillaEliminada);
            await db.SaveChangesAsync();
            plantillaEliminadaId = plantillaEliminada.Id;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", adminEmail);

        var response = await client.GetAsync("/api/v1/plantillasdocumento");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await response.Content.ReadFromJsonAsync<List<PlantillaDocumentoDto>>();
        list.Should().NotBeNull();

        // El admin tampoco debe ver plantillas eliminadas (Bug #6 fix)
        list!.Any(p => p.Id == plantillaEliminadaId).Should().BeFalse(
            "el admin tampoco debe ver plantillas con soft-delete en el listado de administración");
    }
}
