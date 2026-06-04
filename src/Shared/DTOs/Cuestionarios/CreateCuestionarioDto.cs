using System.ComponentModel.DataAnnotations;

namespace TalentManagement.Shared.DTOs.Cuestionarios;

public class CreateCuestionarioDto
{
    [Required(ErrorMessage = "El título es requerido")]
    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    [Range(1, 100, ErrorMessage = "El puntaje debe ser entre 1 y 100")]
    public int PuntajeAprobacion { get; set; } = 70;

    public bool AprobacionPorCorrectas { get; set; }

    [Range(1, 1000, ErrorMessage = "El mínimo de correctas debe ser al menos 1")]
    public int MinCorrectas { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "La cantidad de intentos permitidos debe ser entre 1 y 100")]
    public int IntentosPermitidos { get; set; } = 1;

    [Required]
    public int CapacitacionId { get; set; }

    [MinLength(1, ErrorMessage = "Agrega al menos una pregunta")]
    public List<CreatePreguntaDto> Preguntas { get; set; } = [];
}

public class CreatePreguntaDto
{
    [Required(ErrorMessage = "El enunciado es requerido")]
    public string Enunciado { get; set; } = string.Empty;

    public int Orden { get; set; }

    [MinLength(2, ErrorMessage = "Agrega al menos 2 opciones")]
    public List<CreateOpcionDto> Opciones { get; set; } = [];
}

public class CreateOpcionDto
{
    [Required(ErrorMessage = "El texto de la opción es requerido")]
    public string Texto { get; set; } = string.Empty;

    public bool EsCorrecta { get; set; }
    public int Orden { get; set; }
}
