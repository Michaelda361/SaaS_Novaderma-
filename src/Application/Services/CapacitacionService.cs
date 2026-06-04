using TalentManagement.Application.Interfaces;
using TalentManagement.Domain.Entities;
using TalentManagement.Shared.DTOs.Capacitaciones;

namespace TalentManagement.Application.Services;

public class CapacitacionService(
    ICapacitacionRepository repository,
    IInscripcionRepository inscripcionRepository)
{
    public async Task<List<CapacitacionDto>> GetAllAsync()
    {
        var capacitaciones = await repository.GetAllAsync();
        return capacitaciones.Select(MapToDto).ToList();
    }

    public async Task<CapacitacionDto?> GetByIdAsync(int id)
    {
        var cap = await repository.GetByIdAsync(id);
        return cap is null ? null : MapToDto(cap);
    }

    public async Task<List<CapacitacionDto>> GetByAreaAsync(int areaId)
    {
        var items = await repository.GetByAreaAsync(areaId);
        return items.Select(MapToDto).ToList();
    }

    public async Task<List<CapacitacionDto>> GetByColaboradorAsync(int colaboradorId)
    {
        var items = await repository.GetByColaboradorAsync(colaboradorId);
        return items.Select(MapToDto).ToList();
    }

    public async Task<CapacitacionDto> CreateAsync(CreateCapacitacionDto dto)
    {
        var serverToday = DateTime.Today;
        if (dto.FechaFin.Date < serverToday)
        {
            throw new InvalidOperationException("La fecha límite no puede ser anterior a la fecha de inicio.");
        }

        if (dto.DuracionHoras <= 0)
        {
            throw new InvalidOperationException("La duración de la capacitación debe ser mayor a cero.");
        }

        var capacitacion = new Capacitacion
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            DuracionHoras = dto.DuracionHoras,
            FechaInicio = serverToday,
            FechaFin = dto.FechaFin.Date,
            AreaId = dto.AreaId,
            ColaboradorId = dto.ColaboradorId,
            EmiteCertificado = false,
            NombreCertificado = null,
            PlantillaNombreCertificado = null
        };

        var created = await repository.CreateAsync(capacitacion);

        // Si se asignó a un colaborador específico, inscribirlo automáticamente
        if (dto.ColaboradorId.HasValue)
        {
            var inscripcion = new Inscripcion
            {
                ColaboradorId = dto.ColaboradorId.Value,
                CapacitacionId = created.Id,
                FechaInscripcion = serverToday
            };
            await inscripcionRepository.CreateAsync(inscripcion);
        }

        return MapToDto(created);
    }

    public async Task<CapacitacionDto?> UpdateAsync(int id, CreateCapacitacionDto dto)
    {
        var cap = await repository.GetByIdAsync(id);
        if (cap is null) return null;

        if (dto.FechaFin.Date < cap.FechaInicio.Date)
        {
            throw new InvalidOperationException("La fecha límite no puede ser anterior a la fecha de inicio.");
        }

        if (dto.DuracionHoras <= 0)
        {
            throw new InvalidOperationException("La duración de la capacitación debe ser mayor a cero.");
        }

        cap.Nombre = dto.Nombre;
        cap.Descripcion = dto.Descripcion;
        cap.DuracionHoras = dto.DuracionHoras;
        // Se preserva cap.FechaInicio original
        cap.FechaFin = dto.FechaFin.Date;
        cap.AreaId = dto.AreaId;
        cap.ColaboradorId = dto.ColaboradorId;
        // Se preservan los campos de configuración del certificado

        var updated = await repository.UpdateAsync(cap);
        return MapToDto(updated);
    }

    public async Task<CapacitacionDto?> ConfigurarCertificadoAsync(int id, ConfigurarCertificadoDto dto)
    {
        var cap = await repository.GetByIdAsync(id);
        if (cap is null) return null;
        cap.EmiteCertificado = dto.EmiteCertificado;
        cap.PlantillaNombreCertificado = string.IsNullOrWhiteSpace(dto.PlantillaNombreCertificado) ? null : dto.PlantillaNombreCertificado;

        // Gestionar el DOCX/PPTX
        if (!string.IsNullOrWhiteSpace(dto.ArchivoDocxBase64))
        {
            cap.ArchivoDocxCertificado = Convert.FromBase64String(dto.ArchivoDocxBase64);
            cap.TipoArchivoCertificado = dto.TipoArchivoCertificado;
        }
        else if (dto.EliminarDocx)
        {
            cap.ArchivoDocxCertificado = null;
            cap.TipoArchivoCertificado = null;
        }

        var updated = await repository.UpdateAsync(cap);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var cap = await repository.GetByIdAsync(id);
        if (cap is null) return false;
        await repository.DeleteAsync(id);
        return true;
    }

    public async Task<bool> RestaurarAsync(int id)
    {
        await repository.RestaurarAsync(id);
        return true;
    }

    public async Task<CapacitacionDto?> PublicarAsync(int id)
    {
        var cap = await repository.GetByIdAsync(id);
        if (cap is null) return null;
        cap.Publicada = true;
        var updated = await repository.UpdateAsync(cap);
        return MapToDto(updated);
    }

    public async Task<CapacitacionDto?> DespublicarAsync(int id)
    {
        var cap = await repository.GetByIdAsync(id);
        if (cap is null) return null;
        cap.Publicada = false;
        var updated = await repository.UpdateAsync(cap);
        return MapToDto(updated);
    }

    public async Task<List<CapacitacionDto>> GetActivasAsync()
    {
        var capacitaciones = await repository.GetActivasAsync();
        return capacitaciones.Select(MapToDto).ToList();
    }

    public async Task<List<CapacitacionDto>> GetFinalizadasAsync()
    {
        var capacitaciones = await repository.GetFinalizadasAsync();
        return capacitaciones.Select(MapToDto).ToList();
    }

    public async Task<CapacitacionDto?> FinalizarAutomaticamenteAsync(int capacitacionId)
    {
        var cap = await repository.GetByIdAsync(capacitacionId);
        if (cap is null) return null;

        if (cap.Finalizada) return MapToDto(cap);

        cap.Finalizada = true;
        cap.FechaFinalizacion = DateTime.UtcNow;
        cap.MotivoFinalizacion = "Finalizada automáticamente: todos los colaboradores inscritos completaron su evaluación.";

        var updated = await repository.UpdateAsync(cap);
        return MapToDto(updated);
    }

    /// <summary>Verifica si todos los colaboradores inscritos han respondido el cuestionario.</summary>
    public async Task<bool> TodosCompletaronEvaluacionAsync(int capacitacionId)
    {
        var cap = await repository.GetByIdAsync(capacitacionId);
        if (cap is null) return false;

        // Si no hay inscritos, no se marca como finalizada
        if (!cap.Inscripciones.Any()) return false;

        // Cuestionario sin inscritos = no hay respuestas esperadas
        // Verificar si todos los inscritos respondieron
        var totalInscritos = cap.Inscripciones.Count;
        var totalRespondieron = 0;

        // Nota: aquí usamos InscripcionRepository para contar respuestas
        // Necesitaremos pasar inscripcionRepository al servicio o hacer otra llamada
        return totalRespondieron == totalInscritos;
    }

    private static CapacitacionDto MapToDto(Capacitacion c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Descripcion = c.Descripcion,
        DuracionHoras = c.DuracionHoras,
        FechaInicio = c.FechaInicio,
        FechaFin = c.FechaFin,
        TotalInscritos = c.Inscripciones?.Count ?? 0,
        AreaId = c.AreaId,
        AreaNombre = c.Area?.Nombre,
        ColaboradorId = c.ColaboradorId,
        ColaboradorNombre = c.Colaborador is null ? null : $"{c.Colaborador.Nombre} {c.Colaborador.Apellido}",
        EmiteCertificado = c.EmiteCertificado,
        NombreCertificado = c.NombreCertificado,
        PlantillaNombreCertificado = c.PlantillaNombreCertificado,
        Finalizada = c.Finalizada,
        FechaFinalizacion = c.FechaFinalizacion,
        TienePlantillaDocx = c.ArchivoDocxCertificado is not null && c.ArchivoDocxCertificado.Length > 0,
        TipoArchivoCertificado = c.ArchivoDocxCertificado is { Length: > 0 } ? c.TipoArchivoCertificado : null,
        Publicada = c.Publicada
    };
}
