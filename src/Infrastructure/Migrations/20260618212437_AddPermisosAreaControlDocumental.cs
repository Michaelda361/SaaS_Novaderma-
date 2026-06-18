using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPermisosAreaControlDocumental : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ListadoMaestroPermisos_ListadoMaestroId_ColaboradorId",
                table: "ListadoMaestroPermisos");

            migrationBuilder.AlterColumn<int>(
                name: "ColaboradorId",
                table: "ListadoMaestroPermisos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "AreaId",
                table: "ListadoMaestroPermisos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListadoMaestroPermisos_AreaId",
                table: "ListadoMaestroPermisos",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_ListadoMaestroPermisos_ListadoMaestroId_AreaId",
                table: "ListadoMaestroPermisos",
                columns: new[] { "ListadoMaestroId", "AreaId" },
                unique: true,
                filter: "[AreaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ListadoMaestroPermisos_ListadoMaestroId_ColaboradorId",
                table: "ListadoMaestroPermisos",
                columns: new[] { "ListadoMaestroId", "ColaboradorId" },
                unique: true,
                filter: "[ColaboradorId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ListadoMaestroPermisos_Areas_AreaId",
                table: "ListadoMaestroPermisos",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListadoMaestroPermisos_Areas_AreaId",
                table: "ListadoMaestroPermisos");

            migrationBuilder.DropIndex(
                name: "IX_ListadoMaestroPermisos_AreaId",
                table: "ListadoMaestroPermisos");

            migrationBuilder.DropIndex(
                name: "IX_ListadoMaestroPermisos_ListadoMaestroId_AreaId",
                table: "ListadoMaestroPermisos");

            migrationBuilder.DropIndex(
                name: "IX_ListadoMaestroPermisos_ListadoMaestroId_ColaboradorId",
                table: "ListadoMaestroPermisos");

            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "ListadoMaestroPermisos");

            migrationBuilder.AlterColumn<int>(
                name: "ColaboradorId",
                table: "ListadoMaestroPermisos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListadoMaestroPermisos_ListadoMaestroId_ColaboradorId",
                table: "ListadoMaestroPermisos",
                columns: new[] { "ListadoMaestroId", "ColaboradorId" },
                unique: true);
        }
    }
}
