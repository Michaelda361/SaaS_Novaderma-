using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificadoColaborador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "SolicitudesDocumento",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "NotificadoColaborador",
                table: "SolicitudesDocumento",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Colaboradores",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesDocumento_Estado",
                table: "SolicitudesDocumento",
                column: "Estado",
                filter: "[Activo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Colaboradores_Email",
                table: "Colaboradores",
                column: "Email",
                filter: "[Activo] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SolicitudesDocumento_Estado",
                table: "SolicitudesDocumento");

            migrationBuilder.DropIndex(
                name: "IX_Colaboradores_Email",
                table: "Colaboradores");

            migrationBuilder.DropColumn(
                name: "NotificadoColaborador",
                table: "SolicitudesDocumento");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "SolicitudesDocumento",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Colaboradores",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
