using System.Text.Json.Serialization;
using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class Capacitacion : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int DuracionHoras { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    // Asignaci�n opcional: a un �rea o a un colaborador espec�fico
    public int? AreaId { get; set; }
    public Area? Area { get; set; }

    public int? ColaboradorId { get; set; }
    public Colaborador? Colaborador { get; set; }

    /// <summary>Si true, se emite un certificado autom�ticamente al aprobar el cuestionario.</summary>
    public bool EmiteCertificado { get; set; } = false;

    /// <summary>Nombre del certificado a emitir. Si es null, se usa el nombre de la capacitaci�n.</summary>
    public string? NombreCertificado { get; set; }

    /// <summary>
    /// Plantilla del nombre con variables: {{nombre_completo}}, {{cargo}}, {{area}},
    /// {{capacitacion}}, {{fecha_emision}}, {{puntaje}}.
    /// Si tiene valor, tiene prioridad sobre NombreCertificado.
    /// </summary>
    public string? PlantillaNombreCertificado { get; set; }

    /// <summary>
    /// Archivo DOCX de la plantilla del certificado.
    /// Si est� presente, se genera un PDF personalizado al emitir el certificado.
    /// </summary>
    public byte[]? ArchivoDocxCertificado { get; set; }

    /// <summary>Tipo MIME del archivo de plantilla: DOCX o PPTX. Null = DOCX (legacy).</summary>
    public string? TipoArchivoCertificado { get; set; }

    /// <summary>Archivo PNG de la firma digital para los certificados.</summary>
    public byte[]? ArchivoFirmaCertificado { get; set; }

    /// <summary>Imagen de vista previa del certificado (primer slide de la plantilla convertido a PNG).</summary>
    public byte[]? ArchivoPreviewCertificado { get; set; }


    /// <summary>Coordenada X (izquierda) de la firma en porcentaje respecto al ancho (0-100).</summary>
    public double? FirmaX { get; set; }

    /// <summary>Coordenada Y (superior) de la firma en porcentaje respecto al alto (0-100).</summary>
    public double? FirmaY { get; set; }

    /// <summary>Ancho relativo de la firma en porcentaje respecto al ancho (0-100).</summary>
    public double? FirmaAncho { get; set; }

    /// <summary>Alto relativo de la firma en porcentaje respecto al alto (0-100).</summary>
    public double? FirmaAlto { get; set; }



    /// <summary>
    /// Indica si la capacitaci�n est� publicada y visible para los colaboradores.
    /// Mientras est� en Borrador (false), solo la ven Jefe/Admin.
    /// </summary>
    public bool Publicada { get; set; } = false;

    /// <summary>Se marca true cuando la capacitación se finaliza automáticamente tras aprobar todos los cuestionarios inscritos.</summary>
    public bool Finalizada { get; set; } = false;

    /// <summary>Fecha en la que la capacitación fue finalizada automáticamente.</summary>
    public DateTime? FechaFinalizacion { get; set; }

    /// <summary>Motivo o razón por la cual la capacitación pasó a estado finalizada.</summary>
    public string? MotivoFinalizacion { get; set; }

    [JsonIgnore]
    public ICollection<Inscripcion> Inscripciones { get; set; } = [];
    [JsonIgnore]
    public ICollection<RecursoCapacitacion> Recursos { get; set; } = [];
}
