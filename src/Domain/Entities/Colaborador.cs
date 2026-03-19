using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class Colaborador : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }

    public int AreaId { get; set; }
    public Area Area { get; set; } = null!;

    public int CargoId { get; set; }
    public Cargo Cargo { get; set; } = null!;

    public int? SupervisorId { get; set; }
    public Colaborador? Supervisor { get; set; }

    public ICollection<Certificado> Certificados { get; set; } = [];
    public ICollection<Inscripcion> Inscripciones { get; set; } = [];
}
