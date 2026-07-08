using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TalentManagement.Infrastructure.Persistence.AppDbContext))]
    [Migration("20260408000001_CartasLaboralesMejoras")]
    public partial class CartasLaboralesMejoras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SolicitudDocumento hereda BaseEntity — agregar columna Activo condicionalmente
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('SolicitudesDocumento') 
                      AND name = 'Activo'
                )
                BEGIN
                    ALTER TABLE [SolicitudesDocumento] ADD [Activo] bit NOT NULL DEFAULT CAST(1 AS bit);
                END
            ");

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
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID('SolicitudesDocumento') 
                      AND name = 'Activo'
                )
                BEGIN
                    ALTER TABLE [SolicitudesDocumento] DROP COLUMN [Activo];
                END
            ");

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
