namespace TalentManagement.Client.Helpers;

/// <summary>Texto legible para el género almacenado en API (enum serializado como string).</summary>
public static class ColaboradorGeneroUi
{
    public static string Etiqueta(string? genero) => genero switch
    {
        "Masculino" => "Masculino",
        "Femenino" => "Femenino",
        "OtroOPrefieroNoDecir" => "Otro / Prefiero no decir",
        "NoInformado" or null or "" => "Sin especificar",
        _ => genero.Replace("_", " "),
    };
}
