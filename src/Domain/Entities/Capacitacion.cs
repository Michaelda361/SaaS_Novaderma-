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

    // Asignación opcional: a un área o a un colaborador específico
    public int? AreaId { get; set; }
    public Area? Area { get; set; }

    public int? ColaboradorId { get; set; }
    public Colaborador? Colaborador { get; set; }

    /// <summary>Si true, se emite un certificado automáticamente al aprobar el cuestionario.</summary>
    public bool EmiteCertificado { get; set; } = false;

    /// <summary>Nombre del certificado a emitir. Si es null, se usa el nombre de la capacitación.</summary>
    public string? NombreCertificado { get; set; }

    /// <summary>
    /// Plantilla del nombre con variables: {{nombre_completo}}, {{cargo}}, {{area}},
    /// {{capacitacion}}, {{fecha_emision}}, {{puntaje}}.
    /// Si tiene valor, tiene prioridad sobre NombreCertificado.
    /// </summary>
    public string? PlantillaNombreCertificado { get; set; }

    /// <summary>
    /// Archivo DOCX de la plantilla del certificado.
    /// Si está presente, se genera un PDF personalizado al emitir el certificado.
    /// </summary>
    public byte[]? ArchivoDocxCertificado { get; set; }

    [JsonIgnore]
    public ICollection<Inscripcion> Inscripciones { get; set; } = [];
    [JsonIgnore]
    public ICollection<RecursoCapacitacion> Recursos { get; set; } = [];
}
