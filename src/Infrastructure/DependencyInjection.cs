using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TalentManagement.Application.Interfaces;
using TalentManagement.Application.Services;
using TalentManagement.Infrastructure.Persistence;
using TalentManagement.Infrastructure.Repositories;

namespace TalentManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Repositorios
        services.AddScoped<IColaboradorRepository, ColaboradorRepository>();
        services.AddScoped<ICapacitacionRepository, CapacitacionRepository>();
        services.AddScoped<ICertificadoRepository, CertificadoRepository>();
        services.AddScoped<IAreaRepository, AreaRepository>();
        services.AddScoped<ICargoRepository, CargoRepository>();
        services.AddScoped<IInscripcionRepository, InscripcionRepository>();
        services.AddScoped<IRecursoRepository, RecursoRepository>();

        // Servicios de aplicación
        services.AddScoped<ColaboradorService>();
        services.AddScoped<CapacitacionService>();
        services.AddScoped<CertificadoService>();
        services.AddScoped<AreaService>();
        services.AddScoped<CargoService>();
        services.AddScoped<InscripcionService>();
        services.AddScoped<RecursoService>();

        return services;
    }
}
