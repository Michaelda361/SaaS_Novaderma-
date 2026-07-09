using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReestructurarFichaColaborador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Colaboradores_Colaboradores_SupervisorId",
                table: "Colaboradores");

            migrationBuilder.DropIndex(
                name: "IX_Colaboradores_SupervisorId",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "SupervisorId",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Colaboradores");

            migrationBuilder.RenameColumn(
                name: "Ciudad",
                table: "Colaboradores",
                newName: "LugarNacimiento");

            migrationBuilder.AddColumn<decimal>(
                name: "AuxMediosTransporte",
                table: "Colaboradores",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ComisionCobros",
                table: "Colaboradores",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ComisionVentas",
                table: "Colaboradores",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaExpedicion",
                table: "Colaboradores",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaIngresoContrato",
                table: "Colaboradores",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaNacimiento",
                table: "Colaboradores",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaSalida",
                table: "Colaboradores",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SubTransporte",
                table: "Colaboradores",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuxMediosTransporte",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "ComisionCobros",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "ComisionVentas",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "FechaExpedicion",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "FechaIngresoContrato",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "FechaNacimiento",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "FechaSalida",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "SubTransporte",
                table: "Colaboradores");

            migrationBuilder.RenameColumn(
                name: "LugarNacimiento",
                table: "Colaboradores",
                newName: "Ciudad");

            migrationBuilder.AddColumn<int>(
                name: "SupervisorId",
                table: "Colaboradores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Colaboradores",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_SupervisorId",
                table: "Colaboradores",
                column: "SupervisorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Colaboradores_Colaboradores_SupervisorId",
                table: "Colaboradores",
                column: "SupervisorId",
                principalTable: "Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
