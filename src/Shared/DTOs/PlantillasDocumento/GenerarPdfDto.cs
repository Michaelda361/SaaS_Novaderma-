namespace TalentManagement.Shared.DTOs.PlantillasDocumento;

/// <summary>
/// Campos que el colaborador puede personalizar antes de generar el PDF.
/// Las claves son los nombres de variable sin llaves, ej: "destinatario", "motivo".
/// </summary>
public class GenerarPdfDto
{
    /// <summary>Overrides de variables libres ingresados por el colaborador.</summary>
    public Dictionary<string, string> Extras { get; set; } = [];
}
