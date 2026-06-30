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
using TalentManagement.Shared.DTOs.ControlDocumental;
using Xunit;

namespace TalentManagement.IntegrationTests;

public class ControlDocumentalTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ControlDocumentalTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetSolicitudesPendientes_OnlyReturnsPending()
    {
        var adminEmail = "admin-doc-test@novaderma.com";
        int pendingId;
        int publishedId;

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
                    Apellido = "Doc",
                    Email = adminEmail,
                    Rol = RolUsuario.Admin,
                    AreaId = area.Id,
                    CargoId = cargo.Id,
                    FechaIngreso = DateTime.UtcNow
                };
                db.Colaboradores.Add(admin);
                await db.SaveChangesAsync();
            }

            var listado = new ListadoMaestro
            {
                Nombre = "Listado Test Pendientes",
                Descripcion = "Test",
                Activo = true
            };
            db.ListadosMaestros.Add(listado);
            await db.SaveChangesAsync();

            var doc = new DocumentoControl
            {
                ListadoMaestroId = listado.Id,
                Codigo = "DOC-PND-01",
                Nombre = "Doc Test Pendiente",
                ProcesoResponsable = "Calidad",
                Version = "1.0",
                FechaDocumento = DateTime.Today,
                OneDriveUrl = "https://onedrive.com/doc",
                Estado = "Vigente",
                Activo = true
            };
            db.DocumentosControl.Add(doc);
            await db.SaveChangesAsync();

            // Solicitud A: Pendiente
            var solA = new SolicitudCambioDocumentoControl
            {
                DocumentoControlId = doc.Id,
                SolicitanteId = admin.Id,
                EstadoPropuesta = EstadoPropuesta.PendienteRevision,
                ComentarioSolicitud = "Cambio A",
                DatosPropuestos = "{}"
            };
            db.SolicitudesCambioDocumentoControl.Add(solA);

            // Solicitud B: Publicada (No pendiente)
            var solB = new SolicitudCambioDocumentoControl
            {
                DocumentoControlId = doc.Id,
                SolicitanteId = admin.Id,
                EstadoPropuesta = EstadoPropuesta.Publicada,
                ComentarioSolicitud = "Cambio B",
                DatosPropuestos = "{}"
            };
            db.SolicitudesCambioDocumentoControl.Add(solB);
            await db.SaveChangesAsync();

            pendingId = solA.Id;
            publishedId = solB.Id;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", adminEmail);

        // GET api/v1/control-documental/solicitudes/pendientes
        var response = await client.GetAsync("/api/v1/control-documental/solicitudes/pendientes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await response.Content.ReadFromJsonAsync<List<SolicitudCambioDocumentoControlDto>>();
        list.Should().NotBeNull();
        list.Any(s => s.Id == pendingId).Should().BeTrue();
        list.Any(s => s.Id == publishedId).Should().BeFalse();
    }

    [Fact]
    public async Task JefeOrAuthorizedUser_CanRejectSolicitud()
    {
        var jefeEmail = "jefe-doc-test@novaderma.com";
        var authorizedEmail = "auth-doc-test@novaderma.com";
        int solJefeId;
        int solAuthId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var area = await db.Areas.FirstAsync();
            var cargo = await db.Cargos.FirstAsync();

            // 1. Crear Jefe y asignarlo al área del documento
            var jefe = new Colaborador
            {
                Nombre = "Jefe",
                Apellido = "Doc",
                Email = jefeEmail,
                Rol = RolUsuario.Jefe,
                AreaId = area.Id,
                CargoId = cargo.Id,
                FechaIngreso = DateTime.UtcNow
            };
            db.Colaboradores.Add(jefe);
            await db.SaveChangesAsync();

            area.JefeId = jefe.Id;
            db.Areas.Update(area);
            await db.SaveChangesAsync();

            // 2. Crear usuario autorizado
            var authColab = new Colaborador
            {
                Nombre = "Auth",
                Apellido = "User",
                Email = authorizedEmail,
                Rol = RolUsuario.Colaborador,
                AreaId = area.Id,
                CargoId = cargo.Id,
                FechaIngreso = DateTime.UtcNow
            };
            db.Colaboradores.Add(authColab);
            await db.SaveChangesAsync();

            // 3. Listado Maestro y Documento
            var listado = new ListadoMaestro
            {
                Nombre = "Listado Test Reversiones",
                Activo = true
            };
            db.ListadosMaestros.Add(listado);
            await db.SaveChangesAsync();

            var doc = new DocumentoControl
            {
                ListadoMaestroId = listado.Id,
                Codigo = "DOC-REJ-01",
                Nombre = "Doc Test Rechazo",
                ProcesoResponsable = "Calidad",
                Version = "1.0",
                FechaDocumento = DateTime.Today,
                OneDriveUrl = "https://onedrive.com/doc",
                Estado = "Vigente",
                AreaId = area.Id, // El documento pertenece al área del Jefe
                Activo = true
            };
            db.DocumentosControl.Add(doc);
            await db.SaveChangesAsync();

            // 4. Asignar permiso de "aprobar" al usuario autorizado
            var permiso = new ListadoMaestroPermiso
            {
                ListadoMaestroId = listado.Id,
                ColaboradorId = authColab.Id,
                PuedeVer = true,
                PuedeEditar = true,
                PuedeAprobar = true
            };
            db.ListadoMaestroPermisos.Add(permiso);
            await db.SaveChangesAsync();

            // 5. Crear Solicitudes
            var solJefe = new SolicitudCambioDocumentoControl
            {
                DocumentoControlId = doc.Id,
                SolicitanteId = authColab.Id,
                EstadoPropuesta = EstadoPropuesta.PendienteAprobacion,
                ComentarioSolicitud = "Para Jefe",
                DatosPropuestos = "{}"
            };
            db.SolicitudesCambioDocumentoControl.Add(solJefe);

            var solAuth = new SolicitudCambioDocumentoControl
            {
                DocumentoControlId = doc.Id,
                SolicitanteId = jefe.Id,
                EstadoPropuesta = EstadoPropuesta.PendienteAprobacion,
                ComentarioSolicitud = "Para Auth",
                DatosPropuestos = "{}"
            };
            db.SolicitudesCambioDocumentoControl.Add(solAuth);
            await db.SaveChangesAsync();

            solJefeId = solJefe.Id;
            solAuthId = solAuth.Id;
        }

        // A. Jefe rechaza la solicitud solJefeId -> Debe dar 204
        var clientJefe = _factory.CreateClient();
        clientJefe.DefaultRequestHeaders.Add("X-Test-Email", jefeEmail);

        var rejectDto = new RechazarSolicitudCambioDto { MotivoRechazo = "Rechazado por el Jefe" };
        var resJefe = await clientJefe.PostAsJsonAsync($"/api/v1/control-documental/solicitudes/{solJefeId}/rechazar", rejectDto);
        resJefe.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // B. Usuario Autorizado rechaza la solicitud solAuthId -> Debe dar 204 (gracias a los permisos explícitos)
        var clientAuth = _factory.CreateClient();
        clientAuth.DefaultRequestHeaders.Add("X-Test-Email", authorizedEmail);

        var resAuth = await clientAuth.PostAsJsonAsync($"/api/v1/control-documental/solicitudes/{solAuthId}/rechazar", rejectDto);
        resAuth.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verificar estados finales en base de datos
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sJ = await db.SolicitudesCambioDocumentoControl.FindAsync(solJefeId);
            sJ.Should().NotBeNull();
            sJ!.EstadoPropuesta.Should().Be(EstadoPropuesta.Rechazada);

            var sA = await db.SolicitudesCambioDocumentoControl.FindAsync(solAuthId);
            sA.Should().NotBeNull();
            sA!.EstadoPropuesta.Should().Be(EstadoPropuesta.Rechazada);
        }
    }
}
