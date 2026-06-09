using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddControlDocumentalVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BorradorDocumentoId",
                table: "SolicitudesCambioDocumentoControl",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescripcionDetallada",
                table: "SolicitudesCambioDocumentoControl",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRevision",
                table: "SolicitudesCambioDocumentoControl",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoCambio",
                table: "SolicitudesCambioDocumentoControl",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ObservacionesRevision",
                table: "SolicitudesCambioDocumentoControl",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RevisorId",
                table: "SolicitudesCambioDocumentoControl",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AprobadorId",
                table: "DocumentosControl",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescripcionDetallada",
                table: "DocumentosControl",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocumentoOriginalId",
                table: "DocumentosControl",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EditorId",
                table: "DocumentosControl",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPublicacion",
                table: "DocumentosControl",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoCambio",
                table: "DocumentosControl",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SolicitanteId",
                table: "DocumentosControl",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCambioDocumentoControl_BorradorDocumentoId",
                table: "SolicitudesCambioDocumentoControl",
                column: "BorradorDocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCambioDocumentoControl_RevisorId",
                table: "SolicitudesCambioDocumentoControl",
                column: "RevisorId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosControl_AprobadorId",
                table: "DocumentosControl",
                column: "AprobadorId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosControl_DocumentoOriginalId",
                table: "DocumentosControl",
                column: "DocumentoOriginalId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosControl_EditorId",
                table: "DocumentosControl",
                column: "EditorId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosControl_SolicitanteId",
                table: "DocumentosControl",
                column: "SolicitanteId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentosControl_Colaboradores_AprobadorId",
                table: "DocumentosControl",
                column: "AprobadorId",
                principalTable: "Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentosControl_Colaboradores_EditorId",
                table: "DocumentosControl",
                column: "EditorId",
                principalTable: "Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentosControl_Colaboradores_SolicitanteId",
                table: "DocumentosControl",
                column: "SolicitanteId",
                principalTable: "Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentosControl_DocumentosControl_DocumentoOriginalId",
                table: "DocumentosControl",
                column: "DocumentoOriginalId",
                principalTable: "DocumentosControl",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitudesCambioDocumentoControl_Colaboradores_RevisorId",
                table: "SolicitudesCambioDocumentoControl",
                column: "RevisorId",
                principalTable: "Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitudesCambioDocumentoControl_DocumentosControl_BorradorDocumentoId",
                table: "SolicitudesCambioDocumentoControl",
                column: "BorradorDocumentoId",
                principalTable: "DocumentosControl",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentosControl_Colaboradores_AprobadorId",
                table: "DocumentosControl");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentosControl_Colaboradores_EditorId",
                table: "DocumentosControl");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentosControl_Colaboradores_SolicitanteId",
                table: "DocumentosControl");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentosControl_DocumentosControl_DocumentoOriginalId",
                table: "DocumentosControl");

            migrationBuilder.DropForeignKey(
                name: "FK_SolicitudesCambioDocumentoControl_Colaboradores_RevisorId",
                table: "SolicitudesCambioDocumentoControl");

            migrationBuilder.DropForeignKey(
                name: "FK_SolicitudesCambioDocumentoControl_DocumentosControl_BorradorDocumentoId",
                table: "SolicitudesCambioDocumentoControl");

            migrationBuilder.DropIndex(
                name: "IX_SolicitudesCambioDocumentoControl_BorradorDocumentoId",
                table: "SolicitudesCambioDocumentoControl");

            migrationBuilder.DropIndex(
                name: "IX_SolicitudesCambioDocumentoControl_RevisorId",
                table: "SolicitudesCambioDocumentoControl");

            migrationBuilder.DropIndex(
                name: "IX_DocumentosControl_AprobadorId",
                table: "DocumentosControl");

            migrationBuilder.DropIndex(
                name: "IX_DocumentosControl_DocumentoOriginalId",
                table: "DocumentosControl");

            migrationBuilder.DropIndex(
                name: "IX_DocumentosControl_EditorId",
                table: "DocumentosControl");

            migrationBuilder.DropIndex(
                name: "IX_DocumentosControl_SolicitanteId",
                table: "DocumentosControl");

            migrationBuilder.DropColumn(
                name: "BorradorDocumentoId",
                table: "SolicitudesCambioDocumentoControl");

            migrationBuilder.DropColumn(
                name: "DescripcionDetallada",
                table: "SolicitudesCambioDocumentoControl");

            migrationBuilder.DropColumn(
                name: "FechaRevision",
                table: "SolicitudesCambioDocumentoControl");

            migrationBuilder.DropColumn(
                name: "MotivoCambio",
                table: "SolicitudesCambioDocumentoControl");

            migrationBuilder.DropColumn(
                name: "ObservacionesRevision",
                table: "SolicitudesCambioDocumentoControl");

            migrationBuilder.DropColumn(
                name: "RevisorId",
                table: "SolicitudesCambioDocumentoControl");

            migrationBuilder.DropColumn(
                name: "AprobadorId",
                table: "DocumentosControl");

            migrationBuilder.DropColumn(
                name: "DescripcionDetallada",
                table: "DocumentosControl");

            migrationBuilder.DropColumn(
                name: "DocumentoOriginalId",
                table: "DocumentosControl");

            migrationBuilder.DropColumn(
                name: "EditorId",
                table: "DocumentosControl");

            migrationBuilder.DropColumn(
                name: "FechaPublicacion",
                table: "DocumentosControl");

            migrationBuilder.DropColumn(
                name: "MotivoCambio",
                table: "DocumentosControl");

            migrationBuilder.DropColumn(
                name: "SolicitanteId",
                table: "DocumentosControl");
        }
    }
}
