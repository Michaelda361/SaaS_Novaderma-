using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCapacitacionIdToCertificado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CapacitacionId",
                table: "Certificados",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Certificados_CapacitacionId",
                table: "Certificados",
                column: "CapacitacionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Certificados_Capacitaciones_CapacitacionId",
                table: "Certificados",
                column: "CapacitacionId",
                principalTable: "Capacitaciones",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certificados_Capacitaciones_CapacitacionId",
                table: "Certificados");

            migrationBuilder.DropIndex(
                name: "IX_Certificados_CapacitacionId",
                table: "Certificados");

            migrationBuilder.DropColumn(
                name: "CapacitacionId",
                table: "Certificados");
        }
    }
}
