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
using TalentManagement.Shared.DTOs.Certificados;
using Xunit;

namespace TalentManagement.IntegrationTests;

public class CertificadoTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CertificadoTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCertificados_AsColaborador_ReturnsForbidden()
    {
        var colabEmail = "colab-cert-test@novaderma.com";
        int colabId;
        int targetCertId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var area = await db.Areas.FirstAsync();
            var cargo = await db.Cargos.FirstAsync();

            var colab = new Colaborador
            {
                Nombre = "Colab",
                Apellido = "Cert",
                Email = colabEmail,
                Rol = RolUsuario.Colaborador,
                AreaId = area.Id,
                CargoId = cargo.Id,
                FechaIngreso = DateTime.UtcNow
            };
            db.Colaboradores.Add(colab);

            var anotherColab = new Colaborador
            {
                Nombre = "Another",
                Apellido = "Colab",
                Email = "another-cert-col@novaderma.com",
                Rol = RolUsuario.Colaborador,
                AreaId = area.Id,
                CargoId = cargo.Id,
                FechaIngreso = DateTime.UtcNow
            };
            db.Colaboradores.Add(anotherColab);
            await db.SaveChangesAsync();

            var cert = new Certificado
            {
                Nombre = "Another Cert",
                Institucion = "Institucion",
                FechaEmision = DateTime.Today,
                ColaboradorId = anotherColab.Id,
                Activo = true
            };
            db.Certificados.Add(cert);
            await db.SaveChangesAsync();

            colabId = colab.Id;
            targetCertId = cert.Id;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", colabEmail);

        // 1. GET api/v1/certificados -> Debe dar 403 Forbidden
        var responseGetAll = await client.GetAsync("/api/v1/certificados");
        responseGetAll.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 2. GET api/v1/certificados/vencidos -> Debe dar 403 Forbidden
        var responseVencidos = await client.GetAsync("/api/v1/certificados/vencidos");
        responseVencidos.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. GET api/v1/certificados/proximos-a-vencer -> Debe dar 403 Forbidden
        var responseProximos = await client.GetAsync("/api/v1/certificados/proximos-a-vencer");
        responseProximos.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 4. GET api/v1/certificados/{targetCertId} (Certificado ajeno) -> Debe dar 403 Forbidden
        var responseGetById = await client.GetAsync($"/api/v1/certificados/{targetCertId}");
        responseGetById.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 5. POST api/v1/certificados -> Debe dar 403 Forbidden
        var createDto = new CreateCertificadoDto
        {
            Nombre = "Hack Cert",
            Institucion = "Hack",
            FechaEmision = DateTime.Today,
            ColaboradorId = colabId
        };
        var responsePost = await client.PostAsJsonAsync("/api/v1/certificados", createDto);
        responsePost.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCertificadoById_IncludesColaboradorAndCapacitacion()
    {
        var jefeEmail = "jefe-cert-view@novaderma.com";
        int certId;
        int colabId;
        int capId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var area = await db.Areas.FirstAsync();
            var cargo = await db.Cargos.FirstAsync();

            var jefe = new Colaborador
            {
                Nombre = "Jefe",
                Apellido = "CertView",
                Email = jefeEmail,
                Rol = RolUsuario.Jefe,
                AreaId = area.Id,
                CargoId = cargo.Id,
                FechaIngreso = DateTime.UtcNow
            };
            db.Colaboradores.Add(jefe);

            var colab = new Colaborador
            {
                Nombre = "María",
                Apellido = "Gómez",
                Email = "maria-cert-view@novaderma.com",
                Rol = RolUsuario.Colaborador,
                AreaId = area.Id,
                CargoId = cargo.Id,
                FechaIngreso = DateTime.UtcNow
            };
            db.Colaboradores.Add(colab);
            await db.SaveChangesAsync();

            var cap = new Capacitacion
            {
                Nombre = "Curso de Pruebas",
                DuracionHoras = 20,
                FechaInicio = DateTime.Today,
                FechaFin = DateTime.Today.AddDays(5),
                Activo = true
            };
            db.Capacitaciones.Add(cap);
            await db.SaveChangesAsync();

            var cert = new Certificado
            {
                Nombre = "Certificado de Pruebas",
                Institucion = "NovaHub",
                FechaEmision = DateTime.Today,
                ColaboradorId = colab.Id,
                CapacitacionId = cap.Id,
                Activo = true
            };
            db.Certificados.Add(cert);
            await db.SaveChangesAsync();

            certId = cert.Id;
            colabId = colab.Id;
            capId = cap.Id;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", jefeEmail);

        // GET api/v1/certificados/{id} como Jefe -> Debe ser 200 OK y contener nombres
        var response = await client.GetAsync($"/api/v1/certificados/{certId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<CertificadoDto>();
        dto.Should().NotBeNull();
        dto!.ColaboradorNombre.Should().Be("María Gómez");
        dto.CapacitacionNombre.Should().Be("Curso de Pruebas");
    }

    [Fact]
    public async Task CreateCertificado_InvalidDates_ReturnsBadRequest()
    {
        var jefeEmail = "jefe-cert-date@novaderma.com";
        int colabId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var area = await db.Areas.FirstAsync();
            var cargo = await db.Cargos.FirstAsync();

            var jefe = new Colaborador
            {
                Nombre = "Jefe",
                Apellido = "Date",
                Email = jefeEmail,
                Rol = RolUsuario.Jefe,
                AreaId = area.Id,
                CargoId = cargo.Id,
                FechaIngreso = DateTime.UtcNow
            };
            db.Colaboradores.Add(jefe);

            var colab = new Colaborador
            {
                Nombre = "Colab",
                Apellido = "Date",
                Email = "colab-date-test@novaderma.com",
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
        client.DefaultRequestHeaders.Add("X-Test-Email", jefeEmail);

        // Crear certificado con FechaVencimiento anterior a FechaEmision -> Debe ser 400 BadRequest
        var createDto = new CreateCertificadoDto
        {
            Nombre = "Certificado Invalido",
            Institucion = "Test",
            FechaEmision = DateTime.Today,
            FechaVencimiento = DateTime.Today.AddDays(-1), // Vencimiento anterior
            ColaboradorId = colabId
        };

        var response = await client.PostAsJsonAsync("/api/v1/certificados", createDto);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Jefe_CanDownloadAndRegenerarPdf()
    {
        var jefeEmail = "jefe-cert-action@novaderma.com";
        int certId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var area = await db.Areas.FirstAsync();
            var cargo = await db.Cargos.FirstAsync();

            var jefe = new Colaborador
            {
                Nombre = "Jefe",
                Apellido = "Action",
                Email = jefeEmail,
                Rol = RolUsuario.Jefe,
                AreaId = area.Id,
                CargoId = cargo.Id,
                FechaIngreso = DateTime.UtcNow
            };
            db.Colaboradores.Add(jefe);

            var colab = new Colaborador
            {
                Nombre = "Colab",
                Apellido = "Action",
                Email = "colab-action-test@novaderma.com",
                Rol = RolUsuario.Colaborador,
                AreaId = area.Id,
                CargoId = cargo.Id,
                FechaIngreso = DateTime.UtcNow
            };
            db.Colaboradores.Add(colab);
            await db.SaveChangesAsync();

            var cap = new Capacitacion
            {
                Nombre = "Curso Action",
                DuracionHoras = 10,
                FechaInicio = DateTime.Today,
                FechaFin = DateTime.Today.AddDays(5),
                Activo = true
            };
            db.Capacitaciones.Add(cap);
            await db.SaveChangesAsync();

            var cert = new Certificado
            {
                Nombre = "Certificado Action",
                Institucion = "NovaHub",
                FechaEmision = DateTime.Today,
                ColaboradorId = colab.Id,
                CapacitacionId = cap.Id,
                Activo = true
            };
            db.Certificados.Add(cert);
            await db.SaveChangesAsync();

            certId = cert.Id;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Email", jefeEmail);

        // 1. GET api/v1/certificados/{id}/pdf -> Debe dar NotFound (porque no tiene PDF bytes o archivo real), pero NO Forbidden (403)
        var responsePdf = await client.GetAsync($"/api/v1/certificados/{certId}/pdf");
        responsePdf.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);

        // 2. POST api/v1/certificados/{id}/regenerar-pdf -> Debe ser 200 OK (se genera usando el default CertificatePdfGenerator)
        var responseRegen = await client.PostAsync($"/api/v1/certificados/{certId}/regenerar-pdf", null);
        responseRegen.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
