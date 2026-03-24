using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanupSoftDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RutaCapacitaciones");

            migrationBuilder.DropTable(
                name: "RutasAprendizaje");

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "RecursosCapacitacion",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Inscripciones",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "RecursosCapacitacion");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Inscripciones");

            migrationBuilder.CreateTable(
                name: "RutasAprendizaje",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CargoId = table.Column<int>(type: "int", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RutasAprendizaje", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RutasAprendizaje_Cargos_CargoId",
                        column: x => x.CargoId,
                        principalTable: "Cargos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RutaCapacitaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CapacitacionId = table.Column<int>(type: "int", nullable: false),
                    RutaAprendizajeId = table.Column<int>(type: "int", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RutaCapacitaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RutaCapacitaciones_Capacitaciones_CapacitacionId",
                        column: x => x.CapacitacionId,
                        principalTable: "Capacitaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RutaCapacitaciones_RutasAprendizaje_RutaAprendizajeId",
                        column: x => x.RutaAprendizajeId,
                        principalTable: "RutasAprendizaje",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RutaCapacitaciones_CapacitacionId",
                table: "RutaCapacitaciones",
                column: "CapacitacionId");

            migrationBuilder.CreateIndex(
                name: "IX_RutaCapacitaciones_RutaAprendizajeId",
                table: "RutaCapacitaciones",
                column: "RutaAprendizajeId");

            migrationBuilder.CreateIndex(
                name: "IX_RutasAprendizaje_CargoId",
                table: "RutasAprendizaje",
                column: "CargoId");
        }
    }
}
