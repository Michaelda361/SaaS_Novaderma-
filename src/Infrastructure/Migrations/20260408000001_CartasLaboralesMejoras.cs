using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CartasLaboralesMejoras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SolicitudDocumento hereda BaseEntity — agregar columna Activo
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "SolicitudesDocumento",
                type: "bit",
                nullable: false,
                defaultValue: true);

            // TipoPlantilla cambia de nvarchar a string enum (ya era nvarchar, solo renombrar valores)
            // Los valores existentes "html"/"docx" se mapean a "Html"/"Docx"
            migrationBuilder.Sql(@"
                UPDATE PlantillasDocumento
                SET TipoPlantilla = CASE
                    WHEN TipoPlantilla = 'html' THEN 'Html'
                    WHEN TipoPlantilla = 'docx' THEN 'Docx'
                    ELSE 'Html'
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "SolicitudesDocumento");

            migrationBuilder.Sql(@"
                UPDATE PlantillasDocumento
                SET TipoPlantilla = CASE
                    WHEN TipoPlantilla = 'Html' THEN 'html'
                    WHEN TipoPlantilla = 'Docx' THEN 'docx'
                    ELSE 'html'
                END
            ");
        }
    }
}
