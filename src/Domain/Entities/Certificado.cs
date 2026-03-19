using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class Certificado : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Institucion { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? UrlDocumento { get; set; }

    public int ColaboradorId { get; set; }
    public Colaborador Colaborador { get; set; } = null!;
}
