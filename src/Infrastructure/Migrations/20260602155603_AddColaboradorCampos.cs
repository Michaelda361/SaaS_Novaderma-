using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddColaboradorCampos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ColaboradorCampoDefiniciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampoClave = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Requerido = table.Column<bool>(type: "bit", nullable: false),
                    Opciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColaboradorCampoDefiniciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ColaboradorCampoValores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ColaboradorId = table.Column<int>(type: "int", nullable: false),
                    ColaboradorCampoDefinicionId = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColaboradorCampoValores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ColaboradorCampoValores_ColaboradorCampoDefiniciones_ColaboradorCampoDefinicionId",
                        column: x => x.ColaboradorCampoDefinicionId,
                        principalTable: "ColaboradorCampoDefiniciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ColaboradorCampoValores_Colaboradores_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Colaboradores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ColaboradorCampoValores_ColaboradorCampoDefinicionId",
                table: "ColaboradorCampoValores",
                column: "ColaboradorCampoDefinicionId");

            migrationBuilder.CreateIndex(
                name: "IX_ColaboradorCampoValores_ColaboradorId_ColaboradorCampoDefinicionId",
                table: "ColaboradorCampoValores",
                columns: new[] { "ColaboradorId", "ColaboradorCampoDefinicionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ColaboradorCampoValores");

            migrationBuilder.DropTable(
                name: "ColaboradorCampoDefiniciones");
        }
    }
}
