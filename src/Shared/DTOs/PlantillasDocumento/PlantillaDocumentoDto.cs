namespace TalentManagement.Shared.DTOs.PlantillasDocumento;

public class PlantillaDocumentoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string TipoPlantilla { get; set; } = "html"; // "html" | "docx"
    public string? ContenidoHtml { get; set; }
    public bool TieneDocx { get; set; }
    public string? FirmaImagenBase64 { get; set; }
    public string? NombreFirmante { get; set; }
    public string? CargoFirmante { get; set; }
    public bool AplicaTodasAreas { get; set; }
    public List<int> AreaIds { get; set; } = [];
    public List<string> AreaNombres { get; set; } = [];
    /// <summary>Variables que el colaborador puede editar antes de generar, ej: ["destinatario","motivo"]</summary>
    public List<string> VariablesEditables { get; set; } = [];

    /// <summary>Configuración detallada de campos dinámicos para el formulario.</summary>
    public List<CampoFormularioDto> CamposFormulario { get; set; } = [];

    public bool PermitirCobrosEspeciales { get; set; }
}

