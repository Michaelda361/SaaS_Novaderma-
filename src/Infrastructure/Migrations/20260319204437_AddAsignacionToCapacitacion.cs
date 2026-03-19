using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAsignacionToCapacitacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AreaId",
                table: "Capacitaciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ColaboradorId",
                table: "Capacitaciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Capacitaciones_AreaId",
                table: "Capacitaciones",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Capacitaciones_ColaboradorId",
                table: "Capacitaciones",
                column: "ColaboradorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Capacitaciones_Areas_AreaId",
                table: "Capacitaciones",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Capacitaciones_Colaboradores_ColaboradorId",
                table: "Capacitaciones",
                column: "ColaboradorId",
                principalTable: "Colaboradores",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Capacitaciones_Areas_AreaId",
                table: "Capacitaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Capacitaciones_Colaboradores_ColaboradorId",
                table: "Capacitaciones");

            migrationBuilder.DropIndex(
                name: "IX_Capacitaciones_AreaId",
                table: "Capacitaciones");

            migrationBuilder.DropIndex(
                name: "IX_Capacitaciones_ColaboradorId",
                table: "Capacitaciones");

            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "Capacitaciones");

            migrationBuilder.DropColumn(
                name: "ColaboradorId",
                table: "Capacitaciones");
        }
    }
}
