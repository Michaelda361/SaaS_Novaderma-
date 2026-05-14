using System.ComponentModel.DataAnnotations.Schema;
using TalentManagement.Domain.Common;
using TalentManagement.Domain.Enums;

namespace TalentManagement.Domain.Entities;

public class PlantillaDocumento : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    /// <summary>Html = plantilla HTML con {{variables}} | Docx = archivo Word subido</summary>
    public TipoPlantilla TipoPlantilla { get; set; } = TipoPlantilla.Html;

    /// <summary>Contenido HTML con marcadores {{variable}} (cuando TipoPlantilla = Html)</summary>
    public string? ContenidoHtml { get; set; }

    /// <summary>
    /// Clave en storage del .docx original (cuando TipoPlantilla = Docx).
    /// Formato: "plantillas-docx/{guid}_{nombre.docx}"
    /// </summary>
    public string? DocxFileKey { get; set; }

    // ── Compatibilidad temporal ──────────────────────────────────────────────
    /// <summary>
    /// OBSOLETO — binario heredado. Se mantiene solo para migración de datos viejos.
    /// Nuevas plantillas usan DocxFileKey. Se eliminará en una migración futura.
    /// </summary>
    [Column("ArchivoDocx")]
    public byte[]? ArchivoDocxLegacy { get; set; }

    /// <summary>Imagen de la firma en Base64</summary>
    public string? FirmaImagenBase64 { get; set; }
    public string? NombreFirmante { get; set; }
    public string? CargoFirmante { get; set; }

    /// <summary>true = visible para todos los colaboradores</summary>
    public bool AplicaTodasAreas { get; set; } = true;

    /// <summary>
    /// Variables que el colaborador puede editar antes de generar el PDF.
    /// JSON array de strings, ej: ["destinatario","motivo"]
    /// </summary>
    public string? VariablesEditables { get; set; }

    public ICollection<PlantillaDocumentoArea> Areas { get; set; } = [];
    public ICollection<SolicitudDocumento> Solicitudes { get; set; } = [];
}
