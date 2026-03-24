using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddControlDocumental : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoDocumento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SharePointItemId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SharePointUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AreaId = table.Column<int>(type: "int", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documentos_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RecursosCapacitacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    CapacitacionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecursosCapacitacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecursosCapacitacion_Capacitaciones_CapacitacionId",
                        column: x => x.CapacitacionId,
                        principalTable: "Capacitaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlujosAprobacionDoc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentoId = table.Column<int>(type: "int", nullable: false),
                    EstadoAnterior = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstadoNuevo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ColaboradorId = table.Column<int>(type: "int", nullable: false),
                    FechaTransicion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlujosAprobacionDoc", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlujosAprobacionDoc_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FlujosAprobacionDoc_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PropuestasModificacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentoId = table.Column<int>(type: "int", nullable: false),
                    ColaboradorId = table.Column<int>(type: "int", nullable: false),
                    AreaId = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SharePointItemIdPropuesta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstadoPropuesta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AprobadorId = table.Column<int>(type: "int", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropuestasModificacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropuestasModificacion_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PropuestasModificacion_Colaboradores_AprobadorId",
                        column: x => x.AprobadorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropuestasModificacion_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropuestasModificacion_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VersionesDocumento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentoId = table.Column<int>(type: "int", nullable: false),
                    NumeroVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SharePointItemId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersionesDocumento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VersionesDocumento_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_AreaId",
                table: "Documentos",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_FlujosAprobacionDoc_ColaboradorId",
                table: "FlujosAprobacionDoc",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_FlujosAprobacionDoc_DocumentoId",
                table: "FlujosAprobacionDoc",
                column: "DocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_PropuestasModificacion_AprobadorId",
                table: "PropuestasModificacion",
                column: "AprobadorId");

            migrationBuilder.CreateIndex(
                name: "IX_PropuestasModificacion_AreaId",
                table: "PropuestasModificacion",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_PropuestasModificacion_ColaboradorId",
                table: "PropuestasModificacion",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_PropuestasModificacion_DocumentoId",
                table: "PropuestasModificacion",
                column: "DocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_RecursosCapacitacion_CapacitacionId",
                table: "RecursosCapacitacion",
                column: "CapacitacionId");

            migrationBuilder.CreateIndex(
                name: "IX_VersionesDocumento_DocumentoId",
                table: "VersionesDocumento",
                column: "DocumentoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlujosAprobacionDoc");

            migrationBuilder.DropTable(
                name: "PropuestasModificacion");

            migrationBuilder.DropTable(
                name: "RecursosCapacitacion");

            migrationBuilder.DropTable(
                name: "VersionesDocumento");

            migrationBuilder.DropTable(
                name: "Documentos");
        }
    }
}
