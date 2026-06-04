namespace TalentManagement.Shared.DTOs.Cuestionarios;

public class CuestionarioDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int PuntajeAprobacion { get; set; }
    public bool AprobacionPorCorrectas { get; set; }
    public int MinCorrectas { get; set; }
    public int IntentosPermitidos { get; set; } = 1;
    public int CapacitacionId { get; set; }
    public List<PreguntaDto> Preguntas { get; set; } = [];
}

public class PreguntaDto
{
    public int Id { get; set; }
    public string Enunciado { get; set; } = string.Empty;
    public int Orden { get; set; }
    public List<OpcionDto> Opciones { get; set; } = [];
}

public class OpcionDto
{
    public int Id { get; set; }
    public string Texto { get; set; } = string.Empty;
    public bool EsCorrecta { get; set; }
    public int Orden { get; set; }
}

/// <summary>Payload enviado via SignalR al Jefe cuando un colaborador responde el cuestionario.</summary>
public class CuestionarioRespondidoDto
{
    public string ColaboradorNombre { get; set; } = string.Empty;
    public string CapacitacionNombre { get; set; } = string.Empty;
    public int CapacitacionId { get; set; }
    public decimal Puntaje { get; set; }
    public bool Aprobado { get; set; }
    public int Correctas { get; set; }
    public int TotalPreguntas { get; set; }
}

/// <summary>Payload enviado via SignalR al colaborador cuando se emite su certificado.</summary>
public class CertificadoEmitidoDto
{
    public string NombreCertificado { get; set; } = string.Empty;
    public string CapacitacionNombre { get; set; } = string.Empty;
    public int CapacitacionId { get; set; }
    public decimal Puntaje { get; set; }
    public DateTime FechaEmision { get; set; }
}
