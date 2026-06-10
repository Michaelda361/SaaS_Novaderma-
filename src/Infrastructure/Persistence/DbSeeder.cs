using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TalentManagement.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Areas.AnyAsync()) return;

        var areas = new List<Area>
        {
            new() { Nombre = "Tecnología",        Descripcion = "Desarrollo de software e infraestructura" },
            new() { Nombre = "Recursos Humanos",  Descripcion = "Gestión del talento y bienestar" },
            new() { Nombre = "Finanzas",           Descripcion = "Contabilidad, tesorería y presupuesto" },
            new() { Nombre = "Operaciones",        Descripcion = "Logística y procesos operativos" },
            new() { Nombre = "Comercial",          Descripcion = "Ventas y relaciones con clientes" },
        };

        db.Areas.AddRange(areas);
        await db.SaveChangesAsync();

        var cargos = new List<Cargo>
        {
            new() { Nombre = "Desarrollador Senior",   AreaId = areas[0].Id },
            new() { Nombre = "Desarrollador Junior",   AreaId = areas[0].Id },
            new() { Nombre = "Arquitecto de Software", AreaId = areas[0].Id },
            new() { Nombre = "Analista de RRHH",       AreaId = areas[1].Id },
            new() { Nombre = "Coordinador de RRHH",    AreaId = areas[1].Id },
            new() { Nombre = "Contador",               AreaId = areas[2].Id },
            new() { Nombre = "Analista Financiero",    AreaId = areas[2].Id },
            new() { Nombre = "Jefe de Operaciones",    AreaId = areas[3].Id },
            new() { Nombre = "Ejecutivo Comercial",    AreaId = areas[4].Id },
            new() { Nombre = "Gerente Comercial",      AreaId = areas[4].Id },
        };

        db.Cargos.AddRange(cargos);
        await db.SaveChangesAsync();

        // ── Colaboradores de prueba (dev) ────────────────────────────────────
        var colaboradores = new List<Colaborador>
        {
            // Colaborador en Tecnología
            new() { Nombre = "Andrés",   Apellido = "Martínez Rojas",  Email = "dev.colaborador@test.local",
                    CargoId = cargos[0].Id, AreaId = areas[0].Id,
                    Cedula = "1018456789", Ciudad = "Bogotá",
                    TipoContrato = "Indefinido", SueldoBasico = 4_800_000m,
                    FechaIngreso = new DateTime(2022, 3, 1),
                    Genero = Domain.Enums.GeneroColaborador.Masculino,
                    Rol = Domain.Enums.RolUsuario.Colaborador },

            // Jefe de área Tecnología
            new() { Nombre = "Carlos",   Apellido = "Herrera Ospina",  Email = "dev.jefe@test.local",
                    CargoId = cargos[2].Id, AreaId = areas[0].Id,
                    Cedula = "79654321",  Ciudad = "Medellín",
                    TipoContrato = "Indefinido", SueldoBasico = 9_200_000m,
                    FechaIngreso = new DateTime(2018, 7, 15),
                    Genero = Domain.Enums.GeneroColaborador.Masculino,
                    Rol = Domain.Enums.RolUsuario.Jefe },

            // Jefe de área RRHH — Admin en dev para gestionar plantillas
            new() { Nombre = "Francy",   Apellido = "Gutiérrez Ramírez", Email = "dev.jeferrhh@test.local",
                    CargoId = cargos[4].Id, AreaId = areas[1].Id,
                    Cedula = "52789012",  Ciudad = "Bogotá",
                    TipoContrato = "Indefinido", SueldoBasico = 7_500_000m,
                    FechaIngreso = new DateTime(2016, 2, 1),
                    Genero = Domain.Enums.GeneroColaborador.Femenino,
                    Rol = Domain.Enums.RolUsuario.Admin },
        };

        db.Colaboradores.AddRange(colaboradores);
        await db.SaveChangesAsync();

        // Asignar jefes de área
        areas[0].JefeId = colaboradores[1].Id; // dev.jefe@test.local → Tecnología
        areas[1].JefeId = colaboradores[2].Id; // dev.jeferrhh@test.local → RRHH
        await db.SaveChangesAsync();
    }

    public static async Task CorregirHistoricosHuerfanosAsync(AppDbContext db)
    {
        int correctedCount = 0;

        // 0. Corregir cualquier documento con estado incorrecto "Publicado" a "Vigente"
        var publicados = await db.DocumentosControl
            .Where(d => d.Estado == "Publicado")
            .ToListAsync();

        foreach (var p in publicados)
        {
            p.Estado = "Vigente";
            correctedCount++;
        }

        // 1. Obtener todos los documentos vigentes activos
        var vigentes = await db.DocumentosControl
            .Where(d => d.Activo && d.Estado == "Vigente")
            .ToListAsync();

        foreach (var vigente in vigentes)
        {
            // 2. Buscar registros inactivos del mismo listado y código que no tengan original ID
            var huerfanos = await db.DocumentosControl
                .IgnoreQueryFilters()
                .Where(d => !d.Activo 
                    && d.ListadoMaestroId == vigente.ListadoMaestroId 
                    && d.Codigo == vigente.Codigo 
                    && d.Id != vigente.Id 
                    && d.DocumentoOriginalId == null)
                .ToListAsync();

            foreach (var huerfano in huerfanos)
            {
                huerfano.DocumentoOriginalId = vigente.Id;
                huerfano.Estado = "Histórica";
                correctedCount++;
            }
        }

        if (correctedCount > 0)
        {
            await db.SaveChangesAsync();
            Console.WriteLine($"[MAINTENANCE] Se corrigieron e integraron {correctedCount} registros históricos huérfanos.");
        }
    }

    public static async Task RunIntegrationTestsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<TalentManagement.Application.Interfaces.IControlDocumentalService>();
        var repository = scope.ServiceProvider.GetRequiredService<TalentManagement.Application.Interfaces.IControlDocumentalRepository>();

        Console.WriteLine("[INTEGRATION TEST] Iniciando pruebas del ciclo de vida y versionamiento en Control Documental...");

        // 1. Crear listado maestro temporal de pruebas
        var listado = new ListadoMaestro
        {
            Nombre = "TEST_LISTADO_" + Guid.NewGuid().ToString("N"),
            Descripcion = "Listado de prueba temporal para ciclo de vida"
        };
        db.Set<ListadoMaestro>().Add(listado);
        await db.SaveChangesAsync();

        // 2. Obtener un colaborador para usar como actor (Admin para permitir edición directa)
        var colab = await db.Set<Colaborador>().FirstOrDefaultAsync(c => c.Rol == RolUsuario.Admin)
            ?? throw new InvalidOperationException("No se encontró ningún colaborador Admin para ejecutar la prueba.");

        // 3. Inicializar un documento operativo en estado "Vigente"
        var doc = new DocumentoControl
        {
            ListadoMaestroId = listado.Id,
            Codigo = "TEST-001",
            Nombre = "Documento Original de Prueba",
            ProcesoResponsable = "Tecnología",
            Version = "1.0",
            Estado = "Vigente",
            Activo = true,
            FechaDocumento = DateTime.UtcNow.AddDays(-10),
            OneDriveUrl = "https://onedrive.com/placeholder-original"
        };
        db.Set<DocumentoControl>().Add(doc);
        await db.SaveChangesAsync();

        // 4. Validar que la consulta operativa GetDocumentosAsync devuelva únicamente el Vigente
        var activeDocs = await repository.GetDocumentosAsync(listado.Id, null, null, null, null, null);
        if (activeDocs.Count() != 1 || activeDocs.First().Id != doc.Id)
        {
            throw new Exception("Fallo en Prueba: La consulta operativa GetDocumentosAsync no retornó el documento Vigente de forma exclusiva.");
        }

        // 5. Simular una edición directa realizada por un usuario autorizado (Admin/Jefe)
        var editDto = new TalentManagement.Shared.DTOs.ControlDocumental.UpdateDocumentoControlDto
        {
            ListadoMaestroId = listado.Id,
            Codigo = "TEST-001",
            Nombre = "Documento de Prueba Modificado Directamente",
            ProcesoResponsable = "Tecnología",
            Version = "2.0",
            FechaDocumento = DateTime.UtcNow,
            OneDriveUrl = "https://onedrive.com/placeholder-edited",
            ComentarioCambio = "Primera modificación directa de metadatos"
        };

        var createdDto = await service.UpdateDocumentoAsync(doc.Id, editDto, colab.Email);
        if (createdDto == null)
        {
            throw new Exception("Fallo en Prueba: UpdateDocumentoAsync retornó null.");
        }

        // 6. Verificar que la versión Vigente original NO haya sido modificada
        var originalDocReloaded = await db.Set<DocumentoControl>().AsNoTracking().FirstOrDefaultAsync(d => d.Id == doc.Id);
        if (originalDocReloaded == null || originalDocReloaded.Estado != "Vigente" || !originalDocReloaded.Activo || originalDocReloaded.Nombre != "Documento Original de Prueba")
        {
            throw new Exception("Fallo en Prueba: El documento vigente original fue sobrescrito o desactivado tras la edición.");
        }

        // 7. Verificar que exista la nueva versión en estado "En Revisión"
        var draftDoc = await db.Set<DocumentoControl>().AsNoTracking()
            .FirstOrDefaultAsync(d => d.DocumentoOriginalId == doc.Id && d.Estado == "En Revisión");
        if (draftDoc == null || !draftDoc.Activo || draftDoc.Nombre != "Documento de Prueba Modificado Directamente" || draftDoc.Version != "2.0")
        {
            throw new Exception("Fallo en Prueba: El borrador en estado 'En Revisión' no se generó correctamente.");
        }

        // 8. Verificar que se haya creado la Solicitud de Cambio asociada en estado "PendienteAprobacion"
        var solicitud = await db.Set<SolicitudCambioDocumentoControl>()
            .FirstOrDefaultAsync(s => s.DocumentoControlId == doc.Id && s.BorradorDocumentoId == draftDoc.Id);
        if (solicitud == null || solicitud.EstadoPropuesta != EstadoPropuesta.PendienteAprobacion)
        {
            throw new Exception("Fallo en Prueba: No se registró la solicitud de cambio en estado 'PendienteAprobacion'.");
        }

        // 9. Aprobar la solicitud y verificar el comportamiento de publicación
        await service.AprobarSolicitudCambioAsync(solicitud.Id, "Aprobación de prueba", colab.Email);

        // 9a. El documento original anterior debe haber pasado a "Histórica" y Activo = false
        var previousVigente = await db.Set<DocumentoControl>().IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == doc.Id);
        if (previousVigente == null || previousVigente.Estado != "Histórica" || previousVigente.Activo)
        {
            throw new Exception("Fallo en Prueba: El documento vigente anterior no pasó a 'Histórica' o sigue marcado como activo.");
        }

        // 9b. La versión editada en revisión debe haber pasado a "Vigente" y Activo = true
        var currentVigente = await db.Set<DocumentoControl>().IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == draftDoc.Id);
        if (currentVigente == null || currentVigente.Estado != "Vigente" || !currentVigente.Activo || currentVigente.AprobadorId != colab.Id)
        {
            throw new Exception("Fallo en Prueba: El borrador en revisión no se promovió a 'Vigente' con Activo = true o carece del aprobador.");
        }

        // 9c. El borrador NO debe haber sido eliminado físicamente de la base de datos
        var draftExistsInDb = await db.Set<DocumentoControl>().IgnoreQueryFilters().AnyAsync(d => d.Id == draftDoc.Id);
        if (!draftExistsInDb)
        {
            throw new Exception("Fallo en Prueba: El registro del borrador aprobado fue físicamente eliminado del sistema.");
        }

        // 9d. La solicitud de cambio asociada debe estar en estado "Publicada"
        var resolvedSolicitud = await db.Set<SolicitudCambioDocumentoControl>().FirstOrDefaultAsync(s => s.Id == solicitud.Id);
        if (resolvedSolicitud == null || resolvedSolicitud.EstadoPropuesta != EstadoPropuesta.Publicada)
        {
            throw new Exception("Fallo en Prueba: La solicitud de cambio no quedó marcada como 'Publicada'.");
        }

        // 10. Crear una segunda solicitud de cambio de prueba para validar el rechazo
        var request2 = new SolicitudCambioDocumentoControl
        {
            DocumentoControlId = currentVigente.Id,
            SolicitanteId = colab.Id,
            ComentarioSolicitud = "Modificación de prueba para rechazo",
            MotivoCambio = "Validar comportamiento de rechazo",
            EstadoPropuesta = EstadoPropuesta.PendienteRevision,
            FechaCreacion = DateTime.UtcNow
        };
        db.Set<SolicitudCambioDocumentoControl>().Add(request2);
        await db.SaveChangesAsync();

        // 10a. Iniciar revisión del segundo cambio (creará borrador en En Revisión)
        await service.IniciarRevisionSolicitudAsync(request2.Id, colab.Email);

        var request2Db = await db.Set<SolicitudCambioDocumentoControl>().FirstOrDefaultAsync(s => s.Id == request2.Id);
        if (request2Db == null || !request2Db.BorradorDocumentoId.HasValue)
        {
            throw new Exception("Fallo en Prueba: No se asoció el borrador a la segunda solicitud.");
        }
        var draft2Id = request2Db.BorradorDocumentoId.Value;

        // 10b. Rechazar la segunda solicitud
        await service.RechazarSolicitudCambioAsync(request2.Id, "Rechazo de prueba", colab.Email);

        // 10c. El borrador de la revisión rechazada debe pasar a "Rechazada" y Activo = false
        var rejectedDraft = await db.Set<DocumentoControl>().IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == draft2Id);
        if (rejectedDraft == null || rejectedDraft.Estado != "Rechazada" || rejectedDraft.Activo)
        {
            throw new Exception("Fallo en Prueba: El borrador rechazado no quedó con estado 'Rechazada' o permanece marcado como activo.");
        }

        // 10d. La versión vigente actual debe permanecer inalterada y activa
        var vigentePostRechazo = await db.Set<DocumentoControl>().IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == currentVigente.Id);
        if (vigentePostRechazo == null || vigentePostRechazo.Estado != "Vigente" || !vigentePostRechazo.Activo)
        {
            throw new Exception("Fallo en Prueba: La versión vigente oficial fue alterada o desactivada al rechazar un cambio secundario.");
        }

        // 10e. La segunda solicitud debe estar marcada como "Rechazada" y conservar el enlace del borrador
        var resolvedRequest2 = await db.Set<SolicitudCambioDocumentoControl>().FirstOrDefaultAsync(s => s.Id == request2.Id);
        if (resolvedRequest2 == null || resolvedRequest2.EstadoPropuesta != EstadoPropuesta.Rechazada || !resolvedRequest2.BorradorDocumentoId.HasValue)
        {
            throw new Exception("Fallo en Prueba: La segunda solicitud no quedó en estado 'Rechazada' o perdió el ID del borrador.");
        }

        // 11. Limpieza final de registros de prueba
        db.Set<SolicitudCambioDocumentoControl>().RemoveRange(
            await db.Set<SolicitudCambioDocumentoControl>()
                .IgnoreQueryFilters()
                .Where(s => s.DocumentoControl.ListadoMaestroId == listado.Id)
                .ToListAsync()
        );
        db.Set<DocumentoControl>().RemoveRange(
            await db.Set<DocumentoControl>().IgnoreQueryFilters().Where(d => d.ListadoMaestroId == listado.Id).ToListAsync()
        );
        db.Set<ListadoMaestro>().Remove(listado);
        await db.SaveChangesAsync();

        Console.WriteLine("[INTEGRATION TEST] ¡TODAS LAS PRUEBAS DEL CICLO DE VIDA Y VERSIONAMIENTO PASARON EXITOSAMENTE!");
    }
}
