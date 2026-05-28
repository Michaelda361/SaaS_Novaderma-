namespace TalentManagement.Domain.Enums;

public enum TipoDocumento
{
    Politica,
    Procedimiento,
    Contrato,
    Manual,
    Reglamento
}

public enum EstadoDocumento
{
    Borrador,
    Revision,
    Aprobado,
    Publicado
}

public enum EstadoPropuesta
{
    PendienteRevision,
    EnEdicion,
    PendienteAprobacion,
    Aprobada,
    Rechazada
}
