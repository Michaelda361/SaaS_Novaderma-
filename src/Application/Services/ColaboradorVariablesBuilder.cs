using System.Globalization;
using TalentManagement.Domain.Entities;
using TalentManagement.Domain.Enums;
using TalentManagement.Shared.DTOs.PlantillasDocumento;

namespace TalentManagement.Application.Services;

/// <summary>
/// Constructor de variables para documentos desde los datos del colaborador.
/// Sincroniza automáticamente todos los datos de la ficha de colaborador con las variables disponibles en plantillas.
/// </summary>
public static class ColaboradorVariablesBuilder
{
    private static readonly CultureInfo CulturaEsCo = new CultureInfo("es-CO");

    public static Dictionary<string, string> ConstruirVariablesPerfil(
        Colaborador? colaborador,
        DateTime? fechaReferencia = null,
        Dictionary<string, string?>? camposAdicionales = null,
        bool usarLlavesPlantilla = true)
    {
        var referencia = fechaReferencia ?? DateTime.Today;
        var prefijo = usarLlavesPlantilla ? "{{" : string.Empty;
        var sufijo = usarLlavesPlantilla ? "}}" : string.Empty;

        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Identificación básica
            [$"{prefijo}id{sufijo}"] = colaborador?.Id.ToString() ?? string.Empty,
            [$"{prefijo}nombre{sufijo}"] = colaborador?.Nombre ?? string.Empty,
            [$"{prefijo}apellido{sufijo}"] = colaborador?.Apellido ?? string.Empty,
            [$"{prefijo}nombre_completo{sufijo}"] = FormatearNombreCompleto(colaborador),
            [$"{prefijo}email{sufijo}"] = colaborador?.Email ?? string.Empty,
            [$"{prefijo}cedula{sufijo}"] = colaborador?.Cedula ?? string.Empty,
            
            // Información personal
            [$"{prefijo}fecha_nacimiento{sufijo}"] = FormatearFecha(colaborador?.FechaNacimiento),
            [$"{prefijo}fecha_expedicion{sufijo}"] = FormatearFecha(colaborador?.FechaExpedicion),
            [$"{prefijo}lugar_nacimiento{sufijo}"] = colaborador?.LugarNacimiento ?? string.Empty,
            [$"{prefijo}genero{sufijo}"] = ObtenerTextoGeneroParaDocumento(colaborador?.Genero),
            
            // Información de contrato
            [$"{prefijo}tipo_contrato{sufijo}"] = colaborador?.TipoContrato ?? string.Empty,
            [$"{prefijo}fecha_ingreso{sufijo}"] = FormatearFecha(colaborador?.FechaIngreso),
            [$"{prefijo}fecha_ingreso_contrato{sufijo}"] = FormatearFecha(colaborador?.FechaIngresoContrato),
            [$"{prefijo}fecha_salida{sufijo}"] = FormatearFecha(colaborador?.FechaSalida),
            [$"{prefijo}rol{sufijo}"] = colaborador?.Rol.ToString() ?? string.Empty,
            
            // Información salarial y beneficios
            [$"{prefijo}sueldo_basico{sufijo}"] = FormatearMoneda(colaborador?.SueldoBasico),
            [$"{prefijo}sub_transporte{sufijo}"] = FormatearMoneda(colaborador?.SubTransporte),
            [$"{prefijo}aux_medios_transporte{sufijo}"] = FormatearMoneda(colaborador?.AuxMediosTransporte),
            [$"{prefijo}comision_ventas{sufijo}"] = FormatearMoneda(colaborador?.ComisionVentas),
            [$"{prefijo}comision_cobros{sufijo}"] = FormatearMoneda(colaborador?.ComisionCobros),
            [$"{prefijo}total_salario{sufijo}"] = FormatearMoneda(CalcularTotalSalario(colaborador)),
            
            // Información organizacional
            [$"{prefijo}cargo_nombre{sufijo}"] = colaborador?.Cargo?.Nombre ?? string.Empty,
            [$"{prefijo}cargo{sufijo}"] = colaborador?.Cargo?.Nombre ?? string.Empty,
            [$"{prefijo}area_nombre{sufijo}"] = colaborador?.Area?.Nombre ?? string.Empty,
            [$"{prefijo}area{sufijo}"] = colaborador?.Area?.Nombre ?? string.Empty,
            
            // Documento y firma
            [$"{prefijo}fecha_aprobacion{sufijo}"] = referencia.ToString("dd/MM/yyyy"),
            [$"{prefijo}firma_imagen{sufijo}"] = string.Empty,
            [$"{prefijo}firma_imagen_base64{sufijo}"] = string.Empty,
        };

        AgregarCamposAdicionales(variables, camposAdicionales, usarLlavesPlantilla);
        return variables;
    }

    public static Dictionary<string, string> ConstruirVariablesCertificado(
        Colaborador? colaborador,
        Capacitacion capacitacion,
        DateTime fechaEmision,
        string puntajeStr,
        Dictionary<string, string?>? camposAdicionales = null)
    {
        return ConstruirVariablesPerfil(colaborador, fechaEmision, camposAdicionales, usarLlavesPlantilla: true);
    }

    public static Dictionary<string, string> ConstruirVariablesDocumento(
        Colaborador colaborador,
        PlantillaDocumento plantilla,
        Dictionary<string, string>? extrasFiltrados,
        Dictionary<string, string?>? camposAdicionales = null)
    {
        var variables = ConstruirVariablesPerfil(colaborador, DateTime.Today, camposAdicionales, usarLlavesPlantilla: true);

        if (extrasFiltrados is { Count: > 0 })
        {
            var camposNum = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(plantilla.CamposFormulario))
            {
                try
                {
                    var campos = System.Text.Json.JsonSerializer.Deserialize<List<CampoFormularioDto>>(plantilla.CamposFormulario);
                    if (campos is not null)
                    {
                        foreach (var campo in campos)
                        {
                            if (campo.Tipo == "Número")
                            {
                                camposNum.Add(campo.Variable);
                            }
                        }
                    }
                }
                catch { }
            }

            foreach (var (key, value) in extrasFiltrados)
            {
                if (camposNum.Contains(key))
                {
                    if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue))
                    {
                        variables[$"{{{{{key}}}}}"] = parsedValue.ToString("#,##0", CulturaEsCo);
                        continue;
                    }
                }

                variables[$"{{{{{key}}}}}"] = value ?? string.Empty;
            }
        }

        return variables;
    }

    private static void AgregarCamposAdicionales(
        Dictionary<string, string> variables,
        Dictionary<string, string?>? camposAdicionales,
        bool usarLlavesPlantilla)
    {
        if (camposAdicionales is { Count: > 0 })
        {
            foreach (var (key, value) in camposAdicionales)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    var prefijo = usarLlavesPlantilla ? "{{" : string.Empty;
                    var sufijo = usarLlavesPlantilla ? "}}" : string.Empty;
                    variables[$"{prefijo}{key}{sufijo}"] = value ?? string.Empty;
                }
            }
        }
    }

    private static string FormatearNombreCompleto(Colaborador? colaborador)
    {
        if (colaborador is null) return string.Empty;
        var nombre = (colaborador.Nombre ?? string.Empty).Trim();
        var apellido = (colaborador.Apellido ?? string.Empty).Trim();
        return $"{nombre} {apellido}".Trim();
    }

    private static string FormatearFecha(DateTime? fecha)
    {
        if (!fecha.HasValue || fecha.Value == default) return string.Empty;
        return fecha.Value.ToString("dd/MM/yyyy");
    }

    private static string FormatearMoneda(decimal? valor)
    {
        if (!valor.HasValue || valor.Value == 0) return string.Empty;
        return valor.Value.ToString("$#,##0", CultureInfo.GetCultureInfo("en-US"));
    }

    private static decimal? CalcularTotalSalario(Colaborador? colaborador)
    {
        if (colaborador is null) return null;
        return (colaborador.SueldoBasico ?? 0m)
            + (colaborador.SubTransporte ?? 0m)
            + (colaborador.AuxMediosTransporte ?? 0m)
            + (colaborador.ComisionVentas ?? 0m)
            + (colaborador.ComisionCobros ?? 0m);
    }

    private static string ObtenerTextoGeneroParaDocumento(GeneroColaborador? genero) => genero switch
    {
        GeneroColaborador.Masculino => "el señor",
        GeneroColaborador.Femenino => "la señora",
        _ => "el(la) señor(a)",
    };
}
