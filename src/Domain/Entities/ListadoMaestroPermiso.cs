using TalentManagement.Domain.Common;
using TalentManagement.Domain.Enums;

namespace TalentManagement.Domain.Entities;

public class ListadoMaestroPermiso : BaseEntity
{
    public int ListadoMaestroId { get; set; }
    public ListadoMaestro ListadoMaestro { get; set; } = null!;

    public int ColaboradorId { get; set; }
    public Colaborador Colaborador { get; set; } = null!;

    public bool PuedeVer { get; set; }
    public bool PuedeEditar { get; set; }
    public bool PuedeAprobar { get; set; }
}
