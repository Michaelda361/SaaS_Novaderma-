using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddControlDocumentalMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListadosMaestros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListadosMaestros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosControl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ListadoMaestroId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcesoResponsable = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaDocumento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OneDriveUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OneDriveItemId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArchivoNombre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Uso = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TiempoRetencion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Proteccion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Recuperacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisposicionFinal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComentarioCambio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AreaId = table.Column<int>(type: "int", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosControl", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentosControl_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentosControl_ListadosMaestros_ListadoMaestroId",
                        column: x => x.ListadoMaestroId,
                        principalTable: "ListadosMaestros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosControl_AreaId",
                table: "DocumentosControl",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosControl_ListadoMaestroId",
                table: "DocumentosControl",
                column: "ListadoMaestroId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentosControl");

            migrationBuilder.DropTable(
                name: "ListadosMaestros");
        }
    }
}
