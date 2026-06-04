using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.Cuestionarios;

/// <summary>Payload que envía el colaborador al responder.</summary>
public class ResponderCuestionarioDto
{
    [Required]
    public int CuestionarioId { get; set; }

    [Required]
    public int InscripcionId { get; set; }

    public List<RespuestaPreguntaDto> Respuestas { get; set; } = [];
}

public class RespuestaPreguntaDto
{
    public int PreguntaId { get; set; }
    public int OpcionElegidaId { get; set; }
}

/// <summary>Resultado devuelto al colaborador tras responder.</summary>
public class ResultadoCuestionarioDto
{
    public decimal Puntaje { get; set; }
    public bool Aprobado { get; set; }
    public int PuntajeAprobacion { get; set; }
    public bool AprobacionPorCorrectas { get; set; }
    public int MinCorrectas { get; set; }
    public int TotalPreguntas { get; set; }
    public int Correctas { get; set; }
    public int IntentosMaximos { get; set; }
    public int IntentosRealizados { get; set; }
    public bool PuedeResponderOtroIntento { get; set; }
    public DateTime? FechaFinalizacion { get; set; }

    /// <summary>True si se emitió un certificado automáticamente al aprobar.</summary>
    public bool CertificadoEmitido { get; set; }

    /// <summary>Nombre del certificado emitido, si aplica.</summary>
    public string? NombreCertificado { get; set; }
}
