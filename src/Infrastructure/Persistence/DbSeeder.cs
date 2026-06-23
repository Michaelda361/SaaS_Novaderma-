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
}
