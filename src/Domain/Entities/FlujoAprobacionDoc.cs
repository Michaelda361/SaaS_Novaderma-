using TalentManagement.Domain.Enums;

namespace TalentManagement.Domain.Entities;

public class FlujoAprobacionDoc
{
    public int Id { get; set; }
    public int DocumentoId { get; set; }
    public Documento Documento { get; set; } = null!;

    public EstadoDocumento EstadoAnterior { get; set; }
    public EstadoDocumento EstadoNuevo { get; set; }

    public int ColaboradorId { get; set; }
    public Colaborador Colaborador { get; set; } = null!;

    public DateTime FechaTransicion { get; set; } = DateTime.UtcNow;
}
