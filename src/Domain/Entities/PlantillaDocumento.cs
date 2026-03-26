using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class PlantillaDocumento : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    /// <summary>html = plantilla HTML con {{variables}} | docx = archivo Word subido</summary>
    public string TipoPlantilla { get; set; } = "html";

    /// <summary>Contenido HTML con marcadores {{variable}} (cuando TipoPlantilla = html)</summary>
    public string? ContenidoHtml { get; set; }

    /// <summary>Bytes del .docx original (cuando TipoPlantilla = docx)</summary>
    public byte[]? ArchivoDocx { get; set; }

    /// <summary>Imagen de la firma en Base64</summary>
    public string? FirmaImagenBase64 { get; set; }
    public string? NombreFirmante { get; set; }
    public string? CargoFirmante { get; set; }

    /// <summary>true = visible para todos los colaboradores</summary>
    public bool AplicaTodasAreas { get; set; } = true;

    public ICollection<PlantillaDocumentoArea> Areas { get; set; } = [];
    public ICollection<SolicitudDocumento> Solicitudes { get; set; } = [];
}
