using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class DocumentoControl : BaseEntity
{
    public int ListadoMaestroId { get; set; }
    public ListadoMaestro ListadoMaestro { get; set; } = null!;

    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string ProcesoResponsable { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public DateTime FechaDocumento { get; set; } = DateTime.UtcNow;

    public string OneDriveUrl { get; set; } = string.Empty;
    public string? OneDriveItemId { get; set; }
    public string? ArchivoNombre { get; set; }

    public string? Uso { get; set; }
    public string? TiempoRetencion { get; set; }
    public string? Proteccion { get; set; }
    public string? Recuperacion { get; set; }
    public string? DisposicionFinal { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public string? ComentarioCambio { get; set; }

    private string? _datosPersonalizados;
    public string? DatosPersonalizados
    {
        get => _datosPersonalizados;
        set => _datosPersonalizados = value;
    }

    private static string NormalizarClave(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        return key.Trim().ToLowerInvariant()
            .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
            .Replace("ü", "u").Replace("ñ", "n").Replace("ç", "c").Replace(" ", string.Empty);
    }

    public void SincronizarDeDatosPersonalizados()
    {
        if (string.IsNullOrWhiteSpace(_datosPersonalizados)) return;
        try
        {
            var campos = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string?>>(_datosPersonalizados);
            if (campos == null) return;

            foreach (var kvp in campos)
            {
                var keyNorm = NormalizarClave(kvp.Key);
                var val = kvp.Value?.Trim();
                if (string.IsNullOrWhiteSpace(val)) continue;

                if (keyNorm == "codigo")
                {
                    Codigo = val;
                }
                else if (keyNorm == "nombre")
                {
                    Nombre = val;
                }
                else if (keyNorm == "procesoresponsable" || keyNorm == "proceso" || keyNorm == "responsable")
                {
                    ProcesoResponsable = val;
                }
                else if (keyNorm == "version")
                {
                    Version = val;
                }
                else if (keyNorm == "fechadocumento" || keyNorm == "fecha" || keyNorm == "fechadedocumento")
                {
                    if (DateTime.TryParse(val, out var parsedDate))
                    {
                        FechaDocumento = parsedDate;
                    }
                }
                else if (keyNorm == "uso")
                {
                    Uso = val;
                }
                else if (keyNorm == "tiempoderetenciondelregistro(anos)" || keyNorm == "tiempoderetencion" || keyNorm == "retencion" || keyNorm == "tiemporetencion")
                {
                    TiempoRetencion = val;
                }
                else if (keyNorm == "proteccion")
                {
                    Proteccion = val;
                }
                else if (keyNorm == "recuperacion")
                {
                    Recuperacion = val;
                }
                else if (keyNorm == "disposicionfinal" || keyNorm == "disposicion")
                {
                    DisposicionFinal = val;
                }
                else if (keyNorm == "estado")
                {
                    Estado = val;
                }
                else if (keyNorm == "observaciones")
                {
                    Observaciones = val;
                }
            }
        }
        catch
        {
            // Ignorar errores de análisis JSON
        }
    }

    public void SincronizarHaciaDatosPersonalizados()
    {
        if (string.IsNullOrWhiteSpace(_datosPersonalizados)) return;
        try
        {
            var campos = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string?>>(_datosPersonalizados)
                         ?? new System.Collections.Generic.Dictionary<string, string?>();

            UpdateJsonValue(campos, new[] { "codigo" }, Codigo);
            UpdateJsonValue(campos, new[] { "nombre" }, Nombre);
            UpdateJsonValue(campos, new[] { "procesoresponsable", "proceso", "responsable" }, ProcesoResponsable);
            UpdateJsonValue(campos, new[] { "version" }, Version);
            UpdateJsonValue(campos, new[] { "fechadocumento", "fecha" }, FechaDocumento.ToString("yyyy-MM-dd"));
            UpdateJsonValue(campos, new[] { "uso" }, Uso);
            UpdateJsonValue(campos, new[] { "tiempoderetenciondelregistro(anos)", "tiempoderetencion", "retencion" }, TiempoRetencion);
            UpdateJsonValue(campos, new[] { "proteccion" }, Proteccion);
            UpdateJsonValue(campos, new[] { "recuperacion" }, Recuperacion);
            UpdateJsonValue(campos, new[] { "disposicionfinal", "disposicion" }, DisposicionFinal);
            UpdateJsonValue(campos, new[] { "estado" }, Estado);
            UpdateJsonValue(campos, new[] { "observaciones" }, Observaciones);

            _datosPersonalizados = System.Text.Json.JsonSerializer.Serialize(campos);
        }
        catch
        {
            // Ignorar errores de análisis/serialización JSON
        }
    }

    private void UpdateJsonValue(System.Collections.Generic.Dictionary<string, string?> campos, string[] keys, string? newValue)
    {
        foreach (var key in new System.Collections.Generic.List<string>(campos.Keys))
        {
            var keyNorm = NormalizarClave(key);

            foreach (var candidate in keys)
            {
                var candidateNorm = NormalizarClave(candidate);
                if (keyNorm == candidateNorm)
                {
                    campos[key] = newValue;
                    return;
                }
            }
        }
    }

    public int? AreaId { get; set; }
    public Area? Area { get; set; }

    // Relación autoreferenciada para versiones históricas y borradores
    public int? DocumentoOriginalId { get; set; }
    public DocumentoControl? DocumentoOriginal { get; set; }

    // Trazabilidad del flujo documental
    public int? SolicitanteId { get; set; }
    public Colaborador? Solicitante { get; set; }
    
    public int? EditorId { get; set; }
    public Colaborador? Editor { get; set; }
    
    public int? AprobadorId { get; set; }
    public Colaborador? Aprobador { get; set; }
    
    public DateTime? FechaPublicacion { get; set; }
    public string? MotivoCambio { get; set; }
    public string? DescripcionDetallada { get; set; }
}
