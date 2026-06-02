using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentManagement.Application.Interfaces;
using TalentManagement.Application.Services;
using TalentManagement.Infrastructure.Persistence;
using TalentManagement.Infrastructure.Repositories;
using TalentManagement.Infrastructure.Services;

namespace TalentManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddMemoryCache();

        // Repositorios
        services.AddScoped<IColaboradorRepository, ColaboradorRepository>();
        services.AddScoped<ICapacitacionRepository, CapacitacionRepository>();
        services.AddScoped<ICertificadoRepository, CertificadoRepository>();
        services.AddScoped<IAreaRepository, AreaRepository>();
        services.AddScoped<ICargoRepository, CargoRepository>();
        services.AddScoped<IInscripcionRepository, InscripcionRepository>();
        services.AddScoped<IRecursoRepository, RecursoRepository>();
        services.AddScoped<IDocumentoRepository, DocumentoRepository>();
        services.AddScoped<IControlDocumentalRepository, ControlDocumentalRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IPlantillaDocumentoRepository, PlantillaDocumentoRepository>();
        services.AddScoped<ICuestionarioRepository, CuestionarioRepository>();

        // Servicios de aplicación
        services.AddScoped<ColaboradorService>();
        services.AddScoped<ColaboradorCampoService>();
        services.AddScoped<ControlDocumentalService>();
        services.AddScoped<IControlDocumentalService, ControlDocumentalService>();
        services.AddScoped<CapacitacionService>();
        services.AddScoped<CertificadoService>();
        services.AddScoped<AreaService>();
        services.AddScoped<CargoService>();
        services.AddScoped<InscripcionService>();
        services.AddScoped<RecursoService>();
        services.AddScoped<DocumentoService>();
        services.AddScoped<PlantillaDocumentoService>();
        services.AddScoped<CuestionarioService>();
        services.AddScoped<LibreOfficeConverterService>();
        services.AddScoped<AuditoriaService>();
        services.AddScoped<PdfGeneratorService>();
        services.AddScoped<ICertificadoPdfService, CertificadoPdfService>();
        services.AddScoped<DocxToHtmlConverterService>();

        // SharePoint / FileStorage / AuditExcel — mock en Development, real en producción
        var env = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
        var esDevMode = env == "Development" || !string.IsNullOrWhiteSpace(configuration["DevSettings:DefaultDevUser"]);
        if (esDevMode)
        {
            services.AddScoped<ISharePointService, MockSharePointService>();
            services.AddScoped<IAuditExcelService, MockAuditExcelService>();
            services.AddScoped<IFileStorageService, MockFileStorageService>();
        }
        else
        {
            services.AddScoped<ISharePointService, SharePointService>();
            services.AddScoped<IAuditExcelService, AuditExcelService>();
            services.AddScoped<IFileStorageService, AzureBlobStorageService>();
        }

        services.AddScoped<ICertificatePdfGenerator, CertificatePdfGenerator>();
        return services;
    }
}
