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
using TalentManagement.Shared.DTOs.Capacitaciones;
using TalentManagement.Shared.DTOs.Cuestionarios;
using Xunit;

namespace TalentManagement.IntegrationTests;

public class CapacitacionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CapacitacionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCapacitaciones_AsJefe_BypassesEnrollmentCheck()
    {
        var jefeEmail = "jefe-test-cap@novaderma.com";
        int capId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var area = await db.Areas.FirstAsync();
            var cargo = await db.Cargos.FirstAsync();

            var jefe = await db.Colaboradores.FirstOrDefaultAsync(c => c.Email == jefeEmail);
            if (jefe == null)
            {
                jefe = new Colaborador
                {
                    Nombre = "Jefe",
                    Apellido = "Pruebas",
                    Email = jefeEmail,
                    Rol = RolUsuario.Jefe,
                    AreaId = area.Id,
                    CargoId = cargo.Id,
                    FechaIngreso = DateTime.UtcNow
                };
                db.Colaboradores.Add(jefe);
                await db.SaveChangesAsync();
            }

            var cap = new Capacitacion
            {
                Nombre = "Capacitación Jefe Test",
                Descripcion = "Para pruebas de rol Jefe",
                DuracionHoras = 10,
                FechaInicio = DateTime.Today,
                FechaFin = DateTime.Today.AddDays(5),
                Publicada = true,
                Activo = true
            };
            db.Capacitaciones.Add(cap);
            await db.SaveChangesAsync();
            capId = cap.Id;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", jefeEmail);

        // 1. GET api/v1/capacitaciones (Jefe no inscrito) -> Debe retornar la capacitación
        var responseList = await client.GetAsync("/api/v1/capacitaciones");
        responseList.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await responseList.Content.ReadFromJsonAsync<List<CapacitacionDto>>();
        list.Should().NotBeNull();
        list.Any(c => c.Id == capId).Should().BeTrue();

        // 2. GET api/v1/capacitaciones/{id} -> Debe retornar OK
        var responseDetalle = await client.GetAsync($"/api/v1/capacitaciones/{capId}");
        responseDetalle.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. GET api/v1/recursos/capacitacion/{id} -> Debe retornar OK (vacío o no, pero 200)
        var responseRecursos = await client.GetAsync($"/api/v1/recursos/capacitacion/{capId}");
        responseRecursos.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. GET api/v1/cuestionarios/capacitacion/{id} -> Debe retornar OK o NoContent (pero no Forbidden)
        var responseCuest = await client.GetAsync($"/api/v1/cuestionarios/capacitacion/{capId}");
        responseCuest.StatusCode.Should().Match(s => s == HttpStatusCode.OK || s == HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetResultadoCuestionario_UnauthorizedColaborador_ReturnsForbidden()
    {
        var jefeEmail = "jefe-cuest-test@novaderma.com";
        var colabAEmail = "colab-a-test@novaderma.com";
        var colabBEmail = "colab-b-test@novaderma.com";
        int cuestionarioId;
        int inscripcionAId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var area = await db.Areas.FirstAsync();
            var cargo = await db.Cargos.FirstAsync();

            // Jefe
            var jefe = new Colaborador
            {
                Nombre = "Jefe",
                Apellido = "Cuestionario",
                Email = jefeEmail,
                Rol = RolUsuario.Jefe,
                AreaId = area.Id,
                CargoId = cargo.Id,
                FechaIngreso = DateTime.UtcNow
            };
            db.Colaboradores.Add(jefe);

            // Colab A
            var colabA = new Colaborador
            {
                Nombre = "Colab",
                Apellido = "A",
                Email = colabAEmail,
                Rol = RolUsuario.Colaborador,
                AreaId = area.Id,
                CargoId = cargo.Id,
                FechaIngreso = DateTime.UtcNow
            };
            db.Colaboradores.Add(colabA);

            // Colab B
            var colabB = new Colaborador
            {
                Nombre = "Colab",
                Apellido = "B",
                Email = colabBEmail,
                Rol = RolUsuario.Colaborador,
                AreaId = area.Id,
                CargoId = cargo.Id,
                FechaIngreso = DateTime.UtcNow
            };
            db.Colaboradores.Add(colabB);
            await db.SaveChangesAsync();

            // Capacitacion
            var cap = new Capacitacion
            {
                Nombre = "Capacitacion Cuestionario",
                DuracionHoras = 10,
                FechaInicio = DateTime.Today,
                FechaFin = DateTime.Today.AddDays(5),
                Publicada = true,
                Activo = true
            };
            db.Capacitaciones.Add(cap);
            await db.SaveChangesAsync();

            // Cuestionario
            var cuestionario = new Cuestionario
            {
                Titulo = "Cuestionario Test",
                PuntajeAprobacion = 70,
                IntentosPermitidos = 3,
                CapacitacionId = cap.Id,
                Activo = true,
                Preguntas = new List<Pregunta>
                {
                    new Pregunta
                    {
                        Enunciado = "Pregunta 1",
                        Orden = 1,
                        Activo = true,
                        Opciones = new List<OpcionRespuesta>
                        {
                            new OpcionRespuesta { Texto = "Correcta", EsCorrecta = true, Orden = 1, Activo = true },
                            new OpcionRespuesta { Texto = "Incorrecta", EsCorrecta = false, Orden = 2, Activo = true }
                        }
                    }
                }
            };
            db.Cuestionarios.Add(cuestionario);

            // Inscripcion A
            var inscA = new Inscripcion
            {
                ColaboradorId = colabA.Id,
                CapacitacionId = cap.Id,
                FechaInscripcion = DateTime.UtcNow
            };
            db.Inscripciones.Add(inscA);
            await db.SaveChangesAsync();
            cuestionarioId = cuestionario.Id;
            inscripcionAId = inscA.Id;
        }

        // Responder cuestionario para Colab A
        var clientA = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Add("X-Test-Email", colabAEmail);

        var dtoResponder = new ResponderCuestionarioDto
        {
            CuestionarioId = cuestionarioId,
            InscripcionId = inscripcionAId,
            Respuestas = new List<RespuestaPreguntaDto>()
        };
        // Conseguir id de la pregunta
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var cuestionarioEntity = await db.Cuestionarios.Include(c => c.Preguntas).ThenInclude(p => p.Opciones).FirstAsync(c => c.Id == cuestionarioId);
            var pregunta = cuestionarioEntity.Preguntas.First();
            dtoResponder.Respuestas.Add(new RespuestaPreguntaDto
            {
                PreguntaId = pregunta.Id,
                OpcionElegidaId = pregunta.Opciones.First(o => o.EsCorrecta).Id
            });
        }

        var responseResponder = await clientA.PostAsJsonAsync("/api/v1/cuestionarios/responder", dtoResponder);
        responseResponder.StatusCode.Should().Be(HttpStatusCode.OK);

        // 1. Colab B solicita el resultado de Colab A -> Debe dar 403 Forbidden
        var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Add("X-Test-Email", colabBEmail);
        var responseB = await clientB.GetAsync($"/api/v1/cuestionarios/{cuestionarioId}/resultado/{inscripcionAId}");
        responseB.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 2. Colab A solicita su propio resultado -> Debe dar 200 OK
        var responseA = await clientA.GetAsync($"/api/v1/cuestionarios/{cuestionarioId}/resultado/{inscripcionAId}");
        responseA.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Jefe solicita el resultado de Colab A -> Debe dar 200 OK
        var clientJefe = _factory.CreateClient();
        clientJefe.DefaultRequestHeaders.Add("X-Test-Email", jefeEmail);
        var responseJefe = await clientJefe.GetAsync($"/api/v1/cuestionarios/{cuestionarioId}/resultado/{inscripcionAId}");
        responseJefe.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateCapacitacion_SavesAreaAndColaborador()
    {
        var adminEmail = "admin-cap-create@novaderma.com";
        int areaId;
        int colabId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var area = await db.Areas.FirstAsync();
            areaId = area.Id;
            var cargo = await db.Cargos.FirstAsync();

            var admin = await db.Colaboradores.FirstOrDefaultAsync(c => c.Email == adminEmail);
            if (admin == null)
            {
                admin = new Colaborador
                {
                    Nombre = "Admin",
                    Apellido = "Cap",
                    Email = adminEmail,
                    Rol = RolUsuario.Admin,
                    AreaId = area.Id,
                    CargoId = cargo.Id,
                    FechaIngreso = DateTime.UtcNow
                };
                db.Colaboradores.Add(admin);
            }

            var colab = new Colaborador
            {
                Nombre = "Asignado",
                Apellido = "Prueba",
                Email = "asignado@novaderma.com",
                Rol = RolUsuario.Colaborador,
                AreaId = area.Id,
                CargoId = cargo.Id,
                FechaIngreso = DateTime.UtcNow
            };
            db.Colaboradores.Add(colab);
            await db.SaveChangesAsync();
            colabId = colab.Id;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", adminEmail);

        // 1. Crear capacitacion asignada a Area
        var areaDto = new CreateCapacitacionDto
        {
            Nombre = "Capacitación por Área",
            Descripcion = "Test Área",
            DuracionHoras = 8,
            FechaInicio = DateTime.Today,
            FechaFin = DateTime.Today.AddDays(5),
            AreaId = areaId
        };
        var resArea = await client.PostAsJsonAsync("/api/v1/capacitaciones", areaDto);
        resArea.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdArea = await resArea.Content.ReadFromJsonAsync<CapacitacionDto>();
        createdArea.Should().NotBeNull();
        createdArea!.AreaId.Should().Be(areaId);
        createdArea.ColaboradorId.Should().BeNull();

        // 2. Crear capacitacion asignada a Colaborador
        var colabDto = new CreateCapacitacionDto
        {
            Nombre = "Capacitación por Colaborador",
            Descripcion = "Test Colaborador",
            DuracionHoras = 12,
            FechaInicio = DateTime.Today,
            FechaFin = DateTime.Today.AddDays(5),
            ColaboradorId = colabId
        };
        var resCol = await client.PostAsJsonAsync("/api/v1/capacitaciones", colabDto);
        resCol.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdCol = await resCol.Content.ReadFromJsonAsync<CapacitacionDto>();
        createdCol.Should().NotBeNull();
        createdCol!.ColaboradorId.Should().Be(colabId);
        createdCol.AreaId.Should().BeNull();
    }
}
