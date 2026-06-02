using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class ColaboradorCampoValor : BaseEntity
{
    public int ColaboradorId { get; set; }
    public Colaborador Colaborador { get; set; } = null!;
    public int ColaboradorCampoDefinicionId { get; set; }
    public ColaboradorCampoDefinicion ColaboradorCampoDefinicion { get; set; } = null!;
    public string? Valor { get; set; }
}
