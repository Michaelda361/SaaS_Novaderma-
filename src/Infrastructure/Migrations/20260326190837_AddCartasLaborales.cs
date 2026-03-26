using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCartasLaborales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cedula",
                table: "Colaboradores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ciudad",
                table: "Colaboradores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SueldoBasico",
                table: "Colaboradores",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoContrato",
                table: "Colaboradores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlantillasDocumento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContenidoHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirmaImagenBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreFirmante = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CargoFirmante = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AplicaTodasAreas = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantillasDocumento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlantillaDocumentoAreas",
                columns: table => new
                {
                    PlantillaDocumentoId = table.Column<int>(type: "int", nullable: false),
                    AreaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantillaDocumentoAreas", x => new { x.PlantillaDocumentoId, x.AreaId });
                    table.ForeignKey(
                        name: "FK_PlantillaDocumentoAreas_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlantillaDocumentoAreas_PlantillasDocumento_PlantillaDocumentoId",
                        column: x => x.PlantillaDocumentoId,
                        principalTable: "PlantillasDocumento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesDocumento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantillaDocumentoId = table.Column<int>(type: "int", nullable: false),
                    ColaboradorId = table.Column<int>(type: "int", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesDocumento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesDocumento_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesDocumento_PlantillasDocumento_PlantillaDocumentoId",
                        column: x => x.PlantillaDocumentoId,
                        principalTable: "PlantillasDocumento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlantillaDocumentoAreas_AreaId",
                table: "PlantillaDocumentoAreas",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesDocumento_ColaboradorId",
                table: "SolicitudesDocumento",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesDocumento_PlantillaDocumentoId",
                table: "SolicitudesDocumento",
                column: "PlantillaDocumentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantillaDocumentoAreas");

            migrationBuilder.DropTable(
                name: "SolicitudesDocumento");

            migrationBuilder.DropTable(
                name: "PlantillasDocumento");

            migrationBuilder.DropColumn(
                name: "Cedula",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "Ciudad",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "SueldoBasico",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "TipoContrato",
                table: "Colaboradores");
        }
    }
}
