using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSignatureToCapacitacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "ArchivoFirmaCertificado",
                table: "Capacitaciones",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FirmaAlto",
                table: "Capacitaciones",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FirmaAncho",
                table: "Capacitaciones",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FirmaX",
                table: "Capacitaciones",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FirmaY",
                table: "Capacitaciones",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivoFirmaCertificado",
                table: "Capacitaciones");

            migrationBuilder.DropColumn(
                name: "FirmaAlto",
                table: "Capacitaciones");

            migrationBuilder.DropColumn(
                name: "FirmaAncho",
                table: "Capacitaciones");

            migrationBuilder.DropColumn(
                name: "FirmaX",
                table: "Capacitaciones");

            migrationBuilder.DropColumn(
                name: "FirmaY",
                table: "Capacitaciones");
        }
    }
}
