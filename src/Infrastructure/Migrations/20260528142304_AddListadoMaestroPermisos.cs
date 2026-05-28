using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddListadoMaestroPermisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListadoMaestroPermisos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ListadoMaestroId = table.Column<int>(type: "int", nullable: false),
                    ColaboradorId = table.Column<int>(type: "int", nullable: false),
                    PuedeVer = table.Column<bool>(type: "bit", nullable: false),
                    PuedeEditar = table.Column<bool>(type: "bit", nullable: false),
                    PuedeAprobar = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListadoMaestroPermisos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListadoMaestroPermisos_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ListadoMaestroPermisos_ListadosMaestros_ListadoMaestroId",
                        column: x => x.ListadoMaestroId,
                        principalTable: "ListadosMaestros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListadoMaestroPermisos_ColaboradorId",
                table: "ListadoMaestroPermisos",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_ListadoMaestroPermisos_ListadoMaestroId_ColaboradorId",
                table: "ListadoMaestroPermisos",
                columns: new[] { "ListadoMaestroId", "ColaboradorId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListadoMaestroPermisos");
        }
    }
}
