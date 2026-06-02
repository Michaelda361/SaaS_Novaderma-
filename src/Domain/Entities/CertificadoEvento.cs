namespace TalentManagement.Domain.Entities;

public class CertificadoEvento
{
    public int Id { get; set; }
    public int CertificadoId { get; set; }
    public Certificado Certificado { get; set; } = null!;
    public string Tipo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}
