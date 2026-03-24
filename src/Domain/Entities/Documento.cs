using TalentManagement.Domain.Common;
using TalentManagement.Domain.Enums;

namespace TalentManagement.Domain.Entities;

public class Documento : BaseEntity
{
    public string Titulo { get; set; } = string.Empty;
    public TipoDocumento TipoDocumento { get; set; }
    public string Version { get; set; } = "1.0";
    public EstadoDocumento Estado { get; set; } = EstadoDocumento.Borrador;
    public string SharePointItemId { get; set; } = string.Empty;
    public string SharePointUrl { get; set; } = string.Empty;

    public int? AreaId { get; set; }
    public Area? Area { get; set; }

    public ICollection<VersionDocumento> Versiones { get; set; } = [];
    public ICollection<PropuestaModificacion> Propuestas { get; set; } = [];
    public ICollection<FlujoAprobacionDoc> FlujoAprobacion { get; set; } = [];
}
