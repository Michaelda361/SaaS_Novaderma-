namespace TalentManagement.Domain.Entities;

/// <summary>Tabla intermedia: qué áreas tienen acceso a una plantilla</summary>
public class PlantillaDocumentoArea
{
    public int PlantillaDocumentoId { get; set; }
    public PlantillaDocumento PlantillaDocumento { get; set; } = null!;

    public int AreaId { get; set; }
    public Area Area { get; set; } = null!;
}
