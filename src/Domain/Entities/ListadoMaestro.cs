using TalentManagement.Domain.Common;

namespace TalentManagement.Domain.Entities;

public class ListadoMaestro : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public ICollection<DocumentoControl> Documentos { get; set; } = [];
}
