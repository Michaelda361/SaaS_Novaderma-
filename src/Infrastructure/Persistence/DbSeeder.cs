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
        var colaboradores = new List<Colaborador>
        {
            // Colaborador en Tecnología
            new() { Nombre = "Andrés",   Apellido = "Martínez Rojas",  Email = "dev.colaborador@test.local",
                    CargoId = cargos[0].Id, AreaId = areas[0].Id,
                    Cedula = "1018456789", Ciudad = "Bogotá",
                    TipoContrato = "Indefinido", SueldoBasico = 4_800_000m,
                    FechaIngreso = new DateTime(2022, 3, 1),
                    Rol = Domain.Enums.RolUsuario.Colaborador },

            // Jefe de área Tecnología
            new() { Nombre = "Carlos",   Apellido = "Herrera Ospina",  Email = "dev.jefe@test.local",
                    CargoId = cargos[2].Id, AreaId = areas[0].Id,
                    Cedula = "79654321",  Ciudad = "Medellín",
                    TipoContrato = "Indefinido", SueldoBasico = 9_200_000m,
                    FechaIngreso = new DateTime(2018, 7, 15),
                    Rol = Domain.Enums.RolUsuario.Jefe },

            // Jefe de área RRHH — Admin en dev para gestionar plantillas
            new() { Nombre = "Francy",   Apellido = "Gutiérrez Ramírez", Email = "dev.jeferrhh@test.local",
                    CargoId = cargos[4].Id, AreaId = areas[1].Id,
                    Cedula = "52789012",  Ciudad = "Bogotá",
                    TipoContrato = "Indefinido", SueldoBasico = 7_500_000m,
                    FechaIngreso = new DateTime(2016, 2, 1),
                    Rol = Domain.Enums.RolUsuario.Admin },
        };

        db.Colaboradores.AddRange(colaboradores);
        await db.SaveChangesAsync();

        // Asignar jefes de área
        areas[0].JefeId = colaboradores[1].Id; // dev.jefe@test.local → Tecnología
        areas[1].JefeId = colaboradores[2].Id; // dev.jeferrhh@test.local → RRHH
        await db.SaveChangesAsync();
    }
}
