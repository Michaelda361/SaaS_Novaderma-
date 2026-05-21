using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class DocumentoControlCampoDefinicion : BaseEntity
{
    public int ListadoMaestroId { get; set; }
    public ListadoMaestro ListadoMaestro { get; set; } = null!;

    public string CampoClave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = "Texto";
    public bool Requerido { get; set; }
    public bool EsPredeterminado { get; set; }
    public string? OpcionesJson { get; set; }
    public int Orden { get; set; }
}
