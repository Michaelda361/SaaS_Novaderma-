using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSolicitudCambioDocumentoControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitudesCambioDocumentoControl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentoControlId = table.Column<int>(type: "int", nullable: false),
                    SolicitanteId = table.Column<int>(type: "int", nullable: false),
                    EditorId = table.Column<int>(type: "int", nullable: true),
                    AprobadorId = table.Column<int>(type: "int", nullable: true),
                    ComentarioSolicitud = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComentarioResolucion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatosPropuestos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstadoPropuesta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaEdicion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaResolucion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesCambioDocumentoControl", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesCambioDocumentoControl_Colaboradores_AprobadorId",
                        column: x => x.AprobadorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCambioDocumentoControl_Colaboradores_EditorId",
                        column: x => x.EditorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCambioDocumentoControl_Colaboradores_SolicitanteId",
                        column: x => x.SolicitanteId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesCambioDocumentoControl_DocumentosControl_DocumentoControlId",
                        column: x => x.DocumentoControlId,
                        principalTable: "DocumentosControl",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCambioDocumentoControl_AprobadorId",
                table: "SolicitudesCambioDocumentoControl",
                column: "AprobadorId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCambioDocumentoControl_DocumentoControlId",
                table: "SolicitudesCambioDocumentoControl",
                column: "DocumentoControlId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCambioDocumentoControl_EditorId",
                table: "SolicitudesCambioDocumentoControl",
                column: "EditorId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCambioDocumentoControl_SolicitanteId",
                table: "SolicitudesCambioDocumentoControl",
                column: "SolicitanteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitudesCambioDocumentoControl");
        }
    }
}
