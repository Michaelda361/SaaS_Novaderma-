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
            [$"{prefijo}id{sufijo}"]              = colaborador?.Id.ToString() ?? string.Empty,
            [$"{prefijo}nombre{sufijo}"]          = colaborador?.Nombre ?? string.Empty,
            [$"{prefijo}apellido{sufijo}"]        = colaborador?.Apellido ?? string.Empty,
            [$"{prefijo}nombre_completo{sufijo}"] = FormatearNombreCompleto(colaborador),
            [$"{prefijo}email{sufijo}"]           = colaborador?.Email ?? string.Empty,
            [$"{prefijo}cedula{sufijo}"]          = colaborador?.Cedula ?? string.Empty,

            // Información personal
            [$"{prefijo}fecha_nacimiento{sufijo}"] = FormatearFecha(colaborador?.FechaNacimiento),
            [$"{prefijo}fecha_expedicion{sufijo}"] = FormatearFecha(colaborador?.FechaExpedicion),
            [$"{prefijo}lugar_nacimiento{sufijo}"] = colaborador?.LugarNacimiento ?? string.Empty,
            [$"{prefijo}genero{sufijo}"]           = ObtenerTextoGeneroParaDocumento(colaborador?.Genero),

            // Información de contrato
            [$"{prefijo}tipo_contrato{sufijo}"]         = colaborador?.TipoContrato ?? string.Empty,
            [$"{prefijo}fecha_ingreso{sufijo}"]         = FormatearFecha(colaborador?.FechaIngreso),
            [$"{prefijo}fecha_ingreso_contrato{sufijo}"]= FormatearFecha(colaborador?.FechaIngresoContrato),
            [$"{prefijo}fecha_salida{sufijo}"]          = FormatearFecha(colaborador?.FechaSalida),
            [$"{prefijo}rol{sufijo}"]                   = colaborador?.Rol.ToString() ?? string.Empty,

            // Información salarial y beneficios
            // NOTA: aux_transporte se mantiene como vacío para que plantillas antiguas que lo
            // contengan no muestren el texto literal "{{aux_transporte}}" en el documento.
            [$"{prefijo}aux_transporte{sufijo}"]       = string.Empty,
            [$"{prefijo}sueldo_basico{sufijo}"]        = FormatearMoneda(colaborador?.SueldoBasico),
            [$"{prefijo}sub_transporte{sufijo}"]       = FormatearMoneda(colaborador?.SubTransporte),
            [$"{prefijo}aux_medios_transporte{sufijo}"]= FormatearMoneda(colaborador?.AuxMediosTransporte),
            [$"{prefijo}comision_ventas{sufijo}"]      = FormatearMoneda(colaborador?.ComisionVentas),
            [$"{prefijo}comision_cobros{sufijo}"]      = FormatearMoneda(colaborador?.ComisionCobros),
            [$"{prefijo}total_salario{sufijo}"]        = FormatearMoneda(CalcularTotalSalario(colaborador)),

            // Información organizacional
            [$"{prefijo}cargo_nombre{sufijo}"] = colaborador?.Cargo?.Nombre ?? string.Empty,
            [$"{prefijo}cargo{sufijo}"]        = colaborador?.Cargo?.Nombre ?? string.Empty,
            [$"{prefijo}area_nombre{sufijo}"]  = colaborador?.Area?.Nombre ?? string.Empty,
            [$"{prefijo}area{sufijo}"]         = colaborador?.Area?.Nombre ?? string.Empty,

            // Documento y firma
            [$"{prefijo}fecha_aprobacion{sufijo}"]     = referencia.ToString("dd/MM/yyyy"),
            [$"{prefijo}firma_imagen{sufijo}"]         = string.Empty,
            [$"{prefijo}firma_imagen_base64{sufijo}"]  = string.Empty,
        };

        AgregarCamposAdicionales(variables, camposAdicionales, usarLlavesPlantilla);
        return variables;
    }

    /// <summary>
    /// Construye las variables para plantillas de certificados de capacitación.
    /// Incluye datos del colaborador MÁS variables propias de la capacitación:
    /// {{nombre_capacitacion}}, {{puntaje}}, {{duracion_horas}}, {{fecha_emision}},
    /// {{fecha_emision_larga}}, {{codigo_certificado}}.
    /// </summary>
    public static Dictionary<string, string> ConstruirVariablesCertificado(
        Colaborador? colaborador,
        Capacitacion capacitacion,
        DateTime fechaEmision,
        string puntajeStr,
        Dictionary<string, string?>? camposAdicionales = null)
    {
        // 1. Obtener variables base del colaborador (usando la fecha de emisión como referencia)
        var variables = ConstruirVariablesPerfil(colaborador, fechaEmision, camposAdicionales, usarLlavesPlantilla: true);

        // 2. Agregar variables específicas de la capacitación
        variables["{{nombre_capacitacion}}"]  = capacitacion.Nombre ?? string.Empty;
        variables["{{puntaje}}"]              = puntajeStr ?? string.Empty;
        variables["{{duracion_horas}}"]       = capacitacion.DuracionHoras > 0
                                                    ? capacitacion.DuracionHoras.ToString()
                                                    : string.Empty;
        variables["{{fecha_emision}}"]        = fechaEmision.ToString("dd/MM/yyyy");
        variables["{{fecha_emision_larga}}"]  = FormatearFechaLarga(fechaEmision);
        variables["{{fecha_inicio}}"]         = FormatearFecha(capacitacion.FechaInicio);
        variables["{{fecha_fin}}"]            = FormatearFecha(capacitacion.FechaFin);
        variables["{{descripcion_capacitacion}}"] = capacitacion.Descripcion ?? string.Empty;

        return variables;
    }

    public static Dictionary<string, string> ConstruirVariablesDocumento(
        Colaborador colaborador,
        PlantillaDocumento plantilla,
        Dictionary<string, string>? extrasFiltrados,
        Dictionary<string, string?>? camposAdicionales = null)
    {
        var variables = ConstruirVariablesPerfil(colaborador, DateTime.Today, camposAdicionales, usarLlavesPlantilla: true);

        decimal sueldoBasico = colaborador.SueldoBasico ?? 0m;
        decimal subTransporte = colaborador.SubTransporte ?? 0m;
        decimal auxMediosTransporte = colaborador.AuxMediosTransporte ?? 0m;
        decimal comisionVentas = colaborador.ComisionVentas ?? 0m;
        decimal comisionCobros = colaborador.ComisionCobros ?? 0m;

        bool recalculado = false;

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

            // Si PermitirCobrosEspeciales está activo, tratar virtualmente estos 3 como numéricos
            if (plantilla.PermitirCobrosEspeciales)
            {
                camposNum.Add("aux_medios_transporte");
                camposNum.Add("comision_ventas");
                camposNum.Add("comision_cobros");
            }

            foreach (var (key, value) in extrasFiltrados)
            {
                if (camposNum.Contains(key))
                {
                    // Limpiar puntos de miles colombianos, comas o signo de pesos para poder parsear
                    var cleanValue = value?.Replace(".", "").Replace(",", "").Replace("$", "").Trim() ?? string.Empty;

                    if (decimal.TryParse(cleanValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue))
                    {
                        // Para los cobros especiales, guardamos el valor decimal para recalcular el total
                        if (plantilla.PermitirCobrosEspeciales)
                        {
                            if (string.Equals(key, "aux_medios_transporte", StringComparison.OrdinalIgnoreCase))
                            {
                                auxMediosTransporte = parsedValue;
                                recalculado = true;
                            }
                            else if (string.Equals(key, "comision_ventas", StringComparison.OrdinalIgnoreCase))
                            {
                                comisionVentas = parsedValue;
                                recalculado = true;
                            }
                            else if (string.Equals(key, "comision_cobros", StringComparison.OrdinalIgnoreCase))
                            {
                                comisionCobros = parsedValue;
                                recalculado = true;
                            }
                        }

                        variables[$"{{{{{key}}}}}"] = FormatearMoneda(parsedValue);
                        continue;
                    }
                }

                variables[$"{{{{{key}}}}}"] = value ?? string.Empty;
            }
        }

        // Si se alteró algún cobro especial mediante aprobación, actualizar el total
        if (recalculado)
        {
            var total = sueldoBasico + subTransporte + auxMediosTransporte + comisionVentas + comisionCobros;
            variables["{{total_salario}}"] = FormatearMoneda(total);
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

    /// <summary>
    /// Formatea la fecha en formato largo en español. Ej: "14 de julio de 2026"
    /// </summary>
    private static string FormatearFechaLarga(DateTime fecha)
    {
        var meses = new[]
        {
            "enero", "febrero", "marzo", "abril", "mayo", "junio",
            "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"
        };
        return $"{fecha.Day} de {meses[fecha.Month - 1]} de {fecha.Year}";
    }

    /// <summary>
    /// Formatea un valor monetario con formato colombiano (puntos para miles).
    /// Devuelve string.Empty si el valor es nulo.
    /// Devuelve "$0" si el valor es exactamente 0.
    /// </summary>
    private static string FormatearMoneda(decimal? valor)
    {
        if (!valor.HasValue) return string.Empty;
        // es-CO usa puntos para miles y comas para decimales. 
        // Formateamos sin decimales: "$1.610.833"
        return valor.Value.ToString("$#,##0", CulturaEsCo);
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
        GeneroColaborador.Femenino  => "la señora",
        _                           => "el(la) señor(a)",
    };
}
