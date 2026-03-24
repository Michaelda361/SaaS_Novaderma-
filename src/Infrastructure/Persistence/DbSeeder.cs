using Microsoft.EntityFrameworkCore;
using TalentManagement.Domain.Entities;

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
        // Emails usables via DevSettings:ImpersonateEmail en appsettings.Development.json
        var colaboradores = new List<Colaborador>
        {
            // Colaborador en Finanzas — sin ser jefe de área
            new() { Nombre = "Dev",    Apellido = "Colaborador", Email = "dev.colaborador@test.local",
                    CargoId = cargos[0].Id, AreaId = areas[0].Id },

            // Jefe de área Tecnología — puede aprobar/rechazar propuestas
            new() { Nombre = "Dev",    Apellido = "JefeTech",  Email = "dev.jefe@test.local",
                    CargoId = cargos[2].Id, AreaId = areas[0].Id },

            // Jefe de área RRHH
            new() { Nombre = "Dev",    Apellido = "JefeRRHH",  Email = "dev.jeferrhh@test.local",
                    CargoId = cargos[4].Id, AreaId = areas[1].Id },
        };

        db.Colaboradores.AddRange(colaboradores);
        await db.SaveChangesAsync();

        // Asignar jefes de área
        areas[0].JefeId = colaboradores[1].Id; // dev.jefe@test.local → Tecnología
        areas[1].JefeId = colaboradores[2].Id; // dev.jeferrhh@test.local → RRHH
        await db.SaveChangesAsync();
    }
}
