namespace TalentManagement.Domain.Enums;

/// <summary>
/// Género declarado del colaborador (RRHH). Se usa para variables de carta como {{genero}}.
/// </summary>
public enum GeneroColaborador
{
    /// <summary>Sin dato — se usa texto neutro en documentos.</summary>
    NoInformado = 0,
    Masculino = 1,
    Femenino = 2,
    /// <summary>Incluye identidad no binaria y «prefiero no decir».</summary>
    OtroOPrefieroNoDecir = 3,
}
