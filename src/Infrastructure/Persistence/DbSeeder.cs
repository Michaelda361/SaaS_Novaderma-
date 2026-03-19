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
    }
}
