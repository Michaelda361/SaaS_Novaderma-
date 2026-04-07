using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActivoToMissingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Inscripciones y RecursosCapacitacion heredan BaseEntity pero les faltaba Activo en BD
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Inscripciones",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "RecursosCapacitacion",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Activo", table: "Inscripciones");
            migrationBuilder.DropColumn(name: "Activo", table: "RecursosCapacitacion");
        }
    }
}
