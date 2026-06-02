using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalizacionCapacitacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaFinalizacion",
                table: "Capacitaciones",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Finalizada",
                table: "Capacitaciones",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MotivoFinalizacion",
                table: "Capacitaciones",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaFinalizacion",
                table: "Capacitaciones");

            migrationBuilder.DropColumn(
                name: "Finalizada",
                table: "Capacitaciones");

            migrationBuilder.DropColumn(
                name: "MotivoFinalizacion",
                table: "Capacitaciones");
        }
    }
}
