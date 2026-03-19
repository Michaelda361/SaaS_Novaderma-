namespace TalentManagement.Shared.DTOs.Certificados;

public class CertificadoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Institucion { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? UrlDocumento { get; set; }
    public int ColaboradorId { get; set; }
    public string ColaboradorNombre { get; set; } = string.Empty;
    public bool EstaVencido => FechaVencimiento.HasValue && FechaVencimiento.Value < DateTime.Today;
    public bool VenceProximamente => FechaVencimiento.HasValue
        && !EstaVencido
        && FechaVencimiento.Value <= DateTime.Today.AddDays(30);
}
