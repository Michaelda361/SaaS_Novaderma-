namespace TalentManagement.Shared.DTOs.PlantillasDocumento;

/// <summary>
/// Párrafo del DOCX que contiene al menos una variable — presentado al colaborador
/// como campo de texto editable con el texto ya resuelto.
/// </summary>
public class ParrafoEditableDto
{
    public int Indice { get; set; }
    public string TextoResuelto { get; set; } = string.Empty;
    public string? Contexto { get; set; }
}

public class EditorDocxDto
{
    public List<ParrafoEditableDto> Parrafos { get; set; } = [];
    public string? NombreFirmante { get; set; }
    public string? CargoFirmante { get; set; }
    public string? FirmaImagenBase64 { get; set; }
}

public class GenerarConEdicionDto
{
    /// <summary>Mapa índice de párrafo → texto editado por el colaborador.</summary>
    public Dictionary<int, string> ParrafosEditados { get; set; } = [];
}
