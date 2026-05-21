using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsureDocumentoControlCampoDefiniciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
    IF OBJECT_ID(N'dbo.DocumentoControlCampoDefiniciones') IS NULL
    BEGIN
        CREATE TABLE [dbo].[DocumentoControlCampoDefiniciones](
            [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
            [Activo] bit NOT NULL,
            [CampoClave] nvarchar(max) NOT NULL,
            [EsPredeterminado] bit NOT NULL,
            [ListadoMaestroId] int NOT NULL,
            [Nombre] nvarchar(max) NOT NULL,
            [OpcionesJson] nvarchar(max) NULL,
            [Orden] int NOT NULL,
            [Requerido] bit NOT NULL,
            [Tipo] nvarchar(max) NOT NULL
        );
        IF OBJECT_ID(N'dbo.ListadosMaestros') IS NOT NULL
        BEGIN
            ALTER TABLE [dbo].[DocumentoControlCampoDefiniciones]
            ADD CONSTRAINT FK_DocumentoControlCampoDefiniciones_ListadosMaestros_ListadoMaestroId
            FOREIGN KEY (ListadoMaestroId) REFERENCES [dbo].[ListadosMaestros](Id) ON DELETE CASCADE;
        END
        CREATE INDEX IX_DocumentoControlCampoDefiniciones_ListadoMaestroId
            ON [dbo].[DocumentoControlCampoDefiniciones](ListadoMaestroId);
    END
    ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
    IF OBJECT_ID(N'dbo.DocumentoControlCampoDefiniciones') IS NOT NULL
    BEGIN
        DROP TABLE [dbo].[DocumentoControlCampoDefiniciones];
    END
    ");
        }
    }
}
