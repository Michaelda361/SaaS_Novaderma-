namespace TalentManagement.Shared.DTOs.PlantillasDocumento;

/// <summary>
/// Catálogo de variables estándar disponibles en plantillas de documentos.
/// Todas estas variables se construyen automáticamente desde los datos de la ficha del colaborador.
/// </summary>
public static class PlantillaVariableCatalog
{
    public static readonly IReadOnlyDictionary<string, string> StandardDefinitions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Identificación básica
        ["{{id}}"] = "Identificador único del colaborador",
        ["{{nombre}}"] = "Nombre del colaborador",
        ["{{apellido}}"] = "Apellido del colaborador",
        ["{{nombre_completo}}"] = "Nombre y apellido del colaborador",
        ["{{email}}"] = "Correo electrónico del colaborador",
        ["{{cedula}}"] = "Número de cédula/identificación",
        
        // Información personal
        ["{{fecha_nacimiento}}"] = "Fecha de nacimiento (dd/MM/yyyy)",
        ["{{fecha_expedicion}}"] = "Fecha de expedición de la cédula (dd/MM/yyyy)",
        ["{{lugar_nacimiento}}"] = "Lugar de nacimiento",
        ["{{genero}}"] = "Tratamiento por género (el señor/la señora/el(la) señor(a))",
        
        // Información de contrato
        ["{{tipo_contrato}}"] = "Tipo de contrato (Término fijo, Indefinido, etc.)",
        ["{{fecha_ingreso}}"] = "Fecha de ingreso a la empresa (dd/MM/yyyy)",
        ["{{fecha_ingreso_contrato}}"] = "Fecha de inicio del contrato actual (dd/MM/yyyy)",
        ["{{fecha_salida}}"] = "Fecha de salida/terminación (dd/MM/yyyy, si aplica)",
        ["{{rol}}"] = "Rol del colaborador en la plataforma",
        
        // Información salarial y beneficios
        ["{{sueldo_basico}}"] = "Sueldo básico mensual (formateado con separadores de miles)",
        ["{{sub_transporte}}"] = "Subsidio de transporte (formateado)",
        ["{{aux_medios_transporte}}"] = "Auxilios de medios de transporte (formateado)",
        ["{{aux_transporte}}"] = "Auxilios de transporte (formateado)",
        ["{{comision_ventas}}"] = "Comisión por ventas (formateado)",
        ["{{comision_cobros}}"] = "Comisión por cobros (formateado)",
        ["{{total_salario}}"] = "Total salarial del colaborador (formateado)",
        
        // Información organizacional
        ["{{cargo_nombre}}"] = "Nombre del cargo del colaborador",
        ["{{cargo}}"] = "Nombre del cargo del colaborador",
        ["{{area_nombre}}"] = "Nombre del área/departamento",
        ["{{area}}"] = "Nombre del área/departamento",
        
        // Documento y firma
        ["{{fecha_aprobacion}}"] = "Fecha de aprobación/elaboración del documento (dd/MM/yyyy)",
        ["{{firma_imagen}}"] = "Identificador de firma digital (no es dato de ficha)",
        ["{{firma_imagen_base64}}"] = "Firma digital en formato Base64 (no es dato de ficha)",
    };
}
