using System.Globalization;
using FluentAssertions;
using TalentManagement.Application.Services;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
using TalentManagement.Shared.DTOs.PlantillasDocumento;

namespace TalentManagement.IntegrationTests;

public class ColaboradorVariablesBuilderTests
{
    [Fact]
    public void ConstruirVariablesPerfil_ExponeLosDatosBasicosDelColaboradorConFormatoUnificado()
    {
        var colaborador = new Colaborador
        {
            Id = 42,
            Nombre = "Ana",
            Apellido = "García",
            Email = "ana@example.com",
            Cedula = "1234567890",
            FechaIngreso = new DateTime(2022, 1, 10),
            FechaExpedicion = new DateTime(2021, 5, 12),
            FechaNacimiento = new DateTime(1995, 6, 20),
            LugarNacimiento = "Bogotá",
            TipoContrato = "Término fijo",
            FechaIngresoContrato = new DateTime(2022, 1, 10),
            SueldoBasico = 2000000m,
            SubTransporte = 100000m,
            AuxMediosTransporte = 50000m,
            AuxTransporte = 25000m,
            ComisionVentas = 75000m,
            ComisionCobros = 30000m,
            FechaSalida = null,
            Genero = GeneroColaborador.Femenino,
            Rol = RolUsuario.Colaborador,
            Area = new Area { Id = 1, Nombre = "Operaciones" },
            Cargo = new Cargo { Id = 1, Nombre = "Analista" }
        };

        var variables = ColaboradorVariablesBuilder.ConstruirVariablesPerfil(colaborador, new DateTime(2024, 7, 15));

        // Validar variables básicas
        variables["{{email}}"].Should().Be("ana@example.com");
        variables["{{cedula}}"].Should().Be("1234567890");
        variables["{{genero}}"].Should().Be("la señora");
        variables["{{fecha_aprobacion}}"].Should().Be("15/07/2024");
        
        // Validar identificación completa
        variables["{{id}}"].Should().Be("42");
        variables["{{nombre}}"].Should().Be("Ana");
        variables["{{apellido}}"].Should().Be("García");
        variables["{{nombre_completo}}"].Should().Be("Ana García");
        
        // Validar información personal
        variables["{{fecha_nacimiento}}"].Should().Be("20/06/1995");
        variables["{{lugar_nacimiento}}"].Should().Be("Bogotá");
        
        // Validar información de contrato
        variables["{{tipo_contrato}}"].Should().Be("Término fijo");
        variables["{{fecha_ingreso}}"].Should().Be("10/01/2022");
        variables["{{fecha_ingreso_contrato}}"].Should().Be("10/01/2022");
        variables["{{fecha_salida}}"].Should().Be(string.Empty); // null
        
        // Validar información salarial y beneficios (formateados)
        variables["{{sueldo_basico}}"].Should().Be("$2,000,000");
        variables["{{sub_transporte}}"].Should().Be("$100,000");
        variables["{{aux_medios_transporte}}"].Should().Be("$50,000");
        variables["{{aux_transporte}}"].Should().Be("$25,000");
        variables["{{comision_ventas}}"].Should().Be("$75,000");
        variables["{{comision_cobros}}"].Should().Be("$30,000");
        
        // Validar información organizacional
        variables["{{cargo_nombre}}"].Should().Be("Analista");
        variables["{{area_nombre}}"].Should().Be("Operaciones");
        
        // Validar firma (vacías por defecto)
        variables.Should().ContainKey("{{firma_imagen}}");
        variables.Should().ContainKey("{{firma_imagen_base64}}");
    }

    [Fact]
    public void ConstruirVariablesPerfil_ManejaNulosCorectamente()
    {
        var colaborador = new Colaborador
        {
            Nombre = "Juan",
            Apellido = "Pérez",
            Email = "juan@example.com",
            Genero = GeneroColaborador.Masculino,
            Area = new Area { Nombre = "TI" },
            Cargo = new Cargo { Nombre = "Developer" }
        };

        var variables = ColaboradorVariablesBuilder.ConstruirVariablesPerfil(colaborador, new DateTime(2024, 1, 1));

        // Campos nulos deben ser strings vacíos
        variables["{{cedula}}"].Should().Be(string.Empty);
        variables["{{fecha_nacimiento}}"].Should().Be(string.Empty);
        variables["{{lugar_nacimiento}}"].Should().Be(string.Empty);
        variables["{{tipo_contrato}}"].Should().Be(string.Empty);
        variables["{{fecha_ingreso_contrato}}"].Should().Be(string.Empty);
        variables["{{fecha_salida}}"].Should().Be(string.Empty);
        variables["{{sueldo_basico}}"].Should().Be(string.Empty);
        variables["{{sub_transporte}}"].Should().Be(string.Empty);
    }

    [Fact]
    public void ConstruirVariablesPerfil_ManejaCerosEnDinero()
    {
        var colaborador = new Colaborador
        {
            Nombre = "Test",
            Apellido = "User",
            Email = "test@example.com",
            SueldoBasico = 0m,
            SubTransporte = 0m,
            Area = new Area { Nombre = "HR" },
            Cargo = new Cargo { Nombre = "Manager" }
        };

        var variables = ColaboradorVariablesBuilder.ConstruirVariablesPerfil(colaborador, new DateTime(2024, 1, 1));

        // Ceros en dinero deben ser strings vacíos
        variables["{{sueldo_basico}}"].Should().Be(string.Empty);
        variables["{{sub_transporte}}"].Should().Be(string.Empty);
    }

    [Fact]
    public void CatalogoDeVariables_ExponeLasVariablesEstandarQueElBuilderReemplaza()
    {
        var colaborador = new Colaborador
        {
            Nombre = "Ana",
            Apellido = "García",
            Email = "ana@example.com",
            Cargo = new Cargo { Nombre = "Analista" },
            Area = new Area { Nombre = "Operaciones" }
        };

        var variables = ColaboradorVariablesBuilder.ConstruirVariablesPerfil(colaborador, new DateTime(2024, 7, 15));

        // VERIFICACIÓN CRÍTICA: Todas las variables estándar del catálogo deben estar presentes
        foreach (var variable in PlantillaVariableCatalog.StandardDefinitions.Select(v => v.Key))
        {
            variables.Should().ContainKey(variable, $"La variable {variable} debe estar en el resultado");
        }
    }

    [Fact]
    public void ConstruirVariablesPerfil_UsaFormatoUnificadoParaFechas()
    {
        var colaborador = new Colaborador
        {
            Nombre = "Test",
            Apellido = "User",
            Email = "test@example.com",
            FechaNacimiento = new DateTime(1990, 12, 25),
            FechaIngreso = new DateTime(2020, 3, 15),
            FechaIngresoContrato = new DateTime(2021, 6, 1),
            FechaSalida = new DateTime(2024, 12, 31),
            Area = new Area { Nombre = "Test" },
            Cargo = new Cargo { Nombre = "Test" }
        };

        var variables = ColaboradorVariablesBuilder.ConstruirVariablesPerfil(colaborador, new DateTime(2024, 1, 1));

        // Todas las fechas deben estar en formato dd/MM/yyyy
        variables["{{fecha_nacimiento}}"].Should().Match("*/12/1990");
        variables["{{fecha_ingreso}}"].Should().Match("*/03/2020");
        variables["{{fecha_ingreso_contrato}}"].Should().Match("*/06/2021");
        variables["{{fecha_salida}}"].Should().Match("*/12/2024");
    }

    [Fact]
    public void ConstruirVariablesPerfil_FormateoMonedaColombiana()
    {
        var colaborador = new Colaborador
        {
            Nombre = "Test",
            Apellido = "User",
            Email = "test@example.com",
            SueldoBasico = 3500000m,
            SubTransporte = 140000m,
            AuxMediosTransporte = 75500m,
            ComisionVentas = 1250000.5m,
            Area = new Area { Nombre = "Ventas" },
            Cargo = new Cargo { Nombre = "Vendedor" }
        };

        var variables = ColaboradorVariablesBuilder.ConstruirVariablesPerfil(colaborador, new DateTime(2024, 1, 1));

        // Verificar formato de moneda colombiana con $ y separadores de miles
        variables["{{sueldo_basico}}"].Should().Be("$3,500,000");
        variables["{{sub_transporte}}"].Should().Be("$140,000");
        variables["{{aux_medios_transporte}}"].Should().Be("$75,500");
        variables["{{comision_ventas}}"].Should().Be("$1,250,001"); // Redondeado por el formato
    }

    [Fact]
    public void ConstruirVariablesPerfil_SinLlavesPlantilla()
    {
        var colaborador = new Colaborador
        {
            Nombre = "Ana",
            Apellido = "García",
            Email = "ana@example.com",
            Area = new Area { Nombre = "Operaciones" },
            Cargo = new Cargo { Nombre = "Analista" }
        };

        var variables = ColaboradorVariablesBuilder.ConstruirVariablesPerfil(
            colaborador, 
            new DateTime(2024, 7, 15),
            usarLlavesPlantilla: false);

        // Sin usarLlavesPlantilla = true, las claves no deben tener {{}}
        variables.Should().NotContainKey("{{email}}");
        variables.Should().ContainKey("email");
        variables["email"].Should().Be("ana@example.com");
    }

    [Fact]
    public void ConstruirVariablesPerfil_ConCamposAdicionales()
    {
        var colaborador = new Colaborador
        {
            Nombre = "Ana",
            Apellido = "García",
            Email = "ana@example.com",
            Area = new Area { Nombre = "Operaciones" },
            Cargo = new Cargo { Nombre = "Analista" }
        };

        var camposAdicionales = new Dictionary<string, string?>
        {
            { "titulo_profesional", "Ingeniería de Sistemas" },
            { "experiencia_anos", "5" },
            { "idiomas", "Español, Inglés" }
        };

        var variables = ColaboradorVariablesBuilder.ConstruirVariablesPerfil(
            colaborador,
            new DateTime(2024, 7, 15),
            camposAdicionales);

        // Los campos adicionales deben incluirse en las variables
        variables.Should().ContainKey("{{titulo_profesional}}");
        variables.Should().ContainKey("{{experiencia_anos}}");
        variables.Should().ContainKey("{{idiomas}}");
        variables["{{titulo_profesional}}"].Should().Be("Ingeniería de Sistemas");
        variables["{{experiencia_anos}}"].Should().Be("5");
    }

    [Fact]
    public void ConstruirVariablesPerfil_ExponeCamposEstandarDeLaFicha()
    {
        var colaborador = new Colaborador
        {
            Nombre = "Luis",
            Apellido = "Pérez",
            Email = "luis@example.com",
            Cedula = "987654321",
            FechaExpedicion = new DateTime(2020, 3, 4),
            FechaNacimiento = new DateTime(1992, 8, 10),
            SueldoBasico = 1800000m,
            SubTransporte = 140000m,
            AuxMediosTransporte = 60000m,
            AuxTransporte = 30000m,
            ComisionVentas = 200000m,
            ComisionCobros = 100000m,
            Rol = RolUsuario.Jefe,
            Area = new Area { Nombre = "TI" },
            Cargo = new Cargo { Nombre = "Líder" }
        };

        var variables = ColaboradorVariablesBuilder.ConstruirVariablesPerfil(colaborador, new DateTime(2024, 1, 1));

        variables["{{fecha_expedicion}}"].Should().Be("04/03/2020");
        variables["{{total_salario}}"].Should().Be("$2,330,000");
        variables["{{rol}}"].Should().Be("Jefe");
        variables["{{area}}"].Should().Be("TI");
        variables["{{cargo}}"].Should().Be("Líder");
    }
}
