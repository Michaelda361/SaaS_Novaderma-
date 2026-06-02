using TalentManagement.Shared.DTOs.Certificados;

namespace TalentManagement.Application.Interfaces;

public interface ICertificatePdfGenerator
{
    byte[] Generate(CertificatePdfDataDto data);
}
