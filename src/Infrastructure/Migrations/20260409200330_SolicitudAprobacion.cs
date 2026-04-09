using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SolicitudAprobacion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ComentarioAdmin",
                table: "SolicitudesDocumento",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "SolicitudesDocumento",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Pendiente");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaResolucion",
                table: "SolicitudesDocumento",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PdfBytes",
                table: "SolicitudesDocumento",
                type: "varbinary(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ComentarioAdmin", table: "SolicitudesDocumento");
            migrationBuilder.DropColumn(name: "Estado", table: "SolicitudesDocumento");
            migrationBuilder.DropColumn(name: "FechaResolucion", table: "SolicitudesDocumento");
            migrationBuilder.DropColumn(name: "PdfBytes", table: "SolicitudesDocumento");
        }
    }
}
