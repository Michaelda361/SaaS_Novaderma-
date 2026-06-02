namespace TalentManagement.Shared.DTOs.Certificados;

public class CertificatePdfDataDto
{
    public string ParticipantName { get; set; } = string.Empty;
    public string TrainingName { get; set; } = string.Empty;
    public DateTime IssuedDate { get; set; }
    public int DurationHours { get; set; }
    public string CertificateCode { get; set; } = string.Empty;
}
