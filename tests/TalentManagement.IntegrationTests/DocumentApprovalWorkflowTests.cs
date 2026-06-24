using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
using TalentManagement.Infrastructure.Persistence;
using TalentManagement.Shared.DTOs.ControlDocumental;
using Xunit;

namespace TalentManagement.IntegrationTests;

public class DocumentApprovalWorkflowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DocumentApprovalWorkflowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullDocumentApprovalWorkflow_EndToEnd_Success()
    {
        // ==========================================
        // 1. ARRANGEMENT: Seed Test Users & Data
        // ==========================================
        var colabEmail = "colab-workflow@novaderma.com";
        var adminEmail = "admin-workflow@novaderma.com";

        int listadoId;
        int docOriginalId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var area = await db.Areas.FirstAsync();
            var cargo = await db.Cargos.FirstAsync();

            // Colaborador Solicitante
            var colab = await db.Colaboradores.FirstOrDefaultAsync(c => c.Email == colabEmail);
            if (colab == null)
            {
                colab = new Colaborador
                {
                    Nombre = "Juan",
                    Apellido = "Solicitante",
                    Email = colabEmail,
                    Rol = RolUsuario.Colaborador,
                    AreaId = area.Id,
                    CargoId = cargo.Id,
                    FechaIngreso = DateTime.UtcNow
                };
                db.Colaboradores.Add(colab);
            }

            // Administrador Revisor/Aprobador
            var admin = await db.Colaboradores.FirstOrDefaultAsync(c => c.Email == adminEmail);
            if (admin == null)
            {
                admin = new Colaborador
                {
                    Nombre = "Ana",
                    Apellido = "Administradora",
                    Email = adminEmail,
                    Rol = RolUsuario.Admin,
                    AreaId = area.Id,
                    CargoId = cargo.Id,
                    FechaIngreso = DateTime.UtcNow
                };
                db.Colaboradores.Add(admin);
            }

            // Listado Maestro
            var listado = new ListadoMaestro
            {
                Nombre = "Listado Workflow Integracion",
                Descripcion = "Listado para probar el workflow de aprobacion"
            };
            db.ListadosMaestros.Add(listado);
            await db.SaveChangesAsync();
            listadoId = listado.Id;

            // Permisos de edición para el colaborador
            var permiso = new ListadoMaestroPermiso
            {
                ListadoMaestroId = listadoId,
                ColaboradorId = colab.Id,
                PuedeVer = true,
                PuedeEditar = true
            };
            db.ListadoMaestroPermisos.Add(permiso);

            // Documento Control Original (Vigente y Activo)
            var doc = new DocumentoControl
            {
                ListadoMaestroId = listadoId,
                Codigo = "DOC-WORKFLOW-01",
                Nombre = "Documento Original Workflow",
                ProcesoResponsable = "Calidad",
                Version = "1.0",
                FechaDocumento = DateTime.Today.AddDays(-30),
                OneDriveUrl = "https://onedrive.com/doc-workflow-01",
                Estado = "Vigente",
                Activo = true,
                AreaId = area.Id
            };
            db.DocumentosControl.Add(doc);
            await db.SaveChangesAsync();
            docOriginalId = doc.Id;
        }

        // ==========================================
        // 2. STEP 1: Colaborador Proposes Change
        // ==========================================
        var clientColab = _factory.CreateClient();
        clientColab.DefaultRequestHeaders.Add("X-Test-Email", colabEmail);

        var propDto = new UpdateDocumentoControlDto
        {
            ListadoMaestroId = listadoId,
            Codigo = "DOC-WORKFLOW-01",
            Nombre = "Documento Modificado Workflow", // Nombre cambiado
            ProcesoResponsable = "Calidad",
            Version = "1.1", // Version cambiada
            FechaDocumento = DateTime.Today,
            OneDriveUrl = "https://onedrive.com/doc-workflow-01-modified", // Url cambiada
            Estado = "En Revisión",
            ComentarioCambio = "Propuesta de actualización de metadatos y URL",
            AutoApprove = false // Para que no intente autoaprobar (aunque no tiene permiso de aprobar)
        };

        var responseSol = await clientColab.PostAsJsonAsync($"/api/v1/control-documental/documentos/{docOriginalId}/solicitudes", propDto);
        responseSol.StatusCode.Should().Be(HttpStatusCode.Created);

        var solicitud = await responseSol.Content.ReadFromJsonAsync<SolicitudCambioDocumentoControlDto>();
        solicitud.Should().NotBeNull();
        solicitud!.DocumentoControlId.Should().Be(docOriginalId);
        solicitud.EstadoPropuesta.Should().Be("PendienteRevision");
        solicitud.BorradorDocumentoId.Should().BeNull(); // El borrador no se crea hasta iniciar revision

        int solicitudId = solicitud.Id;

        // ==========================================
        // 3. STEP 2: Admin Starts Revision
        // ==========================================
        var clientAdmin = _factory.CreateClient();
        clientAdmin.DefaultRequestHeaders.Add("X-Test-Email", adminEmail);

        var responseRev = await clientAdmin.PostAsync($"/api/v1/control-documental/solicitudes/{solicitudId}/iniciar-revision", null);
        responseRev.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verificar cambio de estado a EnEdicion y obtener el ID del borrador creado
        int borradorDocId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var solDb = await db.SolicitudesCambioDocumentoControl.FindAsync(solicitudId);
            solDb.Should().NotBeNull();
            solDb!.EstadoPropuesta.Should().Be(EstadoPropuesta.EnEdicion);
            solDb.BorradorDocumentoId.Should().NotBeNull();
            borradorDocId = solDb.BorradorDocumentoId!.Value;

            // Verificar en DB que el borrador fue creado con el estado correcto
            var borrador = await db.DocumentosControl.FindAsync(borradorDocId);
            borrador.Should().NotBeNull();
            borrador!.Estado.Should().Be("En Revisión");
            borrador.Nombre.Should().Be("Documento Original Workflow"); // Copia exacta al iniciar revision
        }

        // ==========================================
        // 4. STEP 3: Admin Updates Draft Document
        // ==========================================
        var updateDraftDto = new UpdateDocumentoControlDto
        {
            ListadoMaestroId = listadoId,
            Codigo = "DOC-WORKFLOW-01",
            Nombre = "Documento Modificado Workflow - Final", // Editamos el nombre en borrador
            ProcesoResponsable = "Calidad",
            Version = "1.2", // Cambiamos la versión a 1.2
            FechaDocumento = DateTime.Today,
            OneDriveUrl = "https://onedrive.com/doc-workflow-01-final",
            Estado = "En Revisión"
        };

        var responseDraftUpdate = await clientAdmin.PutAsJsonAsync($"/api/v1/control-documental/solicitudes/{solicitudId}/borrador", updateDraftDto);
        responseDraftUpdate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verificar en DB los cambios en el borrador
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var borrador = await db.DocumentosControl.FindAsync(borradorDocId);
            borrador.Should().NotBeNull();
            borrador!.Nombre.Should().Be("Documento Modificado Workflow - Final");
            borrador.Version.Should().Be("1.2");
        }

        // ==========================================
        // 5. STEP 4: Admin Sends to Approval
        // ==========================================
        var responseSendApp = await clientAdmin.PostAsync($"/api/v1/control-documental/solicitudes/{solicitudId}/enviar-aprobacion", null);
        responseSendApp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verificar cambio de estado a PendienteAprobacion
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var solDb = await db.SolicitudesCambioDocumentoControl.FindAsync(solicitudId);
            solDb.Should().NotBeNull();
            solDb!.EstadoPropuesta.Should().Be(EstadoPropuesta.PendienteAprobacion);
        }

        // ==========================================
        // 6. STEP 5: Admin Approves/Publishes Change
        // ==========================================
        var approveDto = new AprobarSolicitudCambioDto
        {
            Comentarios = "Aprobado satisfactoriamente en pruebas de integracion"
        };

        var responseApprove = await clientAdmin.PostAsJsonAsync($"/api/v1/control-documental/solicitudes/{solicitudId}/aprobar", approveDto);
        responseApprove.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // ==========================================
        // 7. ASSERTIONS: Verify final state of documents and requests
        // ==========================================
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // A. La solicitud debe estar marcada como Publicada (o Aprobada si ese fuera el final, en el controller se usa Publicada en base a la lógica)
            var solDb = await db.SolicitudesCambioDocumentoControl.FindAsync(solicitudId);
            solDb.Should().NotBeNull();
            solDb!.EstadoPropuesta.Should().Be(EstadoPropuesta.Publicada);
            solDb.ComentarioResolucion.Should().Contain("Aprobado satisfactoriamente");

            // B. El documento original anterior debe estar inactivo y marcado como Histórica
            var originalDoc = await db.DocumentosControl.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == docOriginalId);
            originalDoc.Should().NotBeNull();
            originalDoc!.Activo.Should().BeFalse();
            originalDoc.Estado.Should().Be("Histórica");

            // C. El nuevo borrador debe haberse convertido en el documento activo (Vigente y Activo)
            // Espera, ¿cómo maneja el service la aprobación final?
            // En ControlDocumentalService.cs:
            // "solicitud.EstadoPropuesta = EstadoPropuesta.Publicada;"
            // Y actualiza el borrador para que sea Vigente y Activo.
            // Verifiquemos el nuevo documento vigente.
            var nuevoVigente = await db.DocumentosControl.FindAsync(borradorDocId);
            nuevoVigente.Should().NotBeNull();
            nuevoVigente!.Activo.Should().BeTrue();
            nuevoVigente.Estado.Should().Be("Vigente");
            nuevoVigente.Nombre.Should().Be("Documento Modificado Workflow - Final");
            nuevoVigente.Version.Should().Be("1.2");
        }
    }
}
