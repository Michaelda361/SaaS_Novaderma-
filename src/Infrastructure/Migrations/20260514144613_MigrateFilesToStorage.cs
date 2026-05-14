using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MigrateFilesToStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PdfFileKey",
                table: "SolicitudesDocumento",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocxFileKey",
                table: "PlantillasDocumento",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfFileKey",
                table: "Certificados",
                type: "nvarchar(max)",
                nullable: true);

            // ── Estrategia de datos viejos ────────────────────────────────────────
            // Los binarios existentes (ArchivoDocx, PdfBytes) se mantienen en SQL
            // como columnas legacy hasta que sean migrados al storage.
            //
            // El flujo de lectura en PlantillaDocumentoService usa:
            //   1. DocxFileKey / PdfFileKey  → storage (nuevo)
            //   2. ArchivoDocxLegacy / PdfBytes → SQL (fallback para datos viejos)
            //
            // Para migrar los datos viejos al storage, ejecutar el script
            // src/Infrastructure/Scripts/MigrateFilesToStorage.sql
            // DESPUÉS de configurar la conexión al storage en producción.
            //
            // Las columnas legacy (ArchivoDocx en PlantillasDocumento y PdfBytes en
            // SolicitudesDocumento y Certificados) se eliminarán en una migración
            // futura una vez confirmado que todos los registros tienen FileKey poblado.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfFileKey",
                table: "SolicitudesDocumento");

            migrationBuilder.DropColumn(
                name: "DocxFileKey",
                table: "PlantillasDocumento");

            migrationBuilder.DropColumn(
                name: "PdfFileKey",
                table: "Certificados");
        }
    }
}
