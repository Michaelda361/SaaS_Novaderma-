using TalentManagement.Domain.Common;
using TalentManagement.Domain.Enums;

namespace TalentManagement.Domain.Entities;

public class ColaboradorCampoDefinicion : BaseEntity
{
    public string CampoClave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public ColaboradorCampoTipo Tipo { get; set; } = ColaboradorCampoTipo.Texto;
    public bool Requerido { get; set; }
    public string? Opciones { get; set; }
    public int Orden { get; set; }

    public ICollection<ColaboradorCampoValor> Valores { get; set; } = [];
}
