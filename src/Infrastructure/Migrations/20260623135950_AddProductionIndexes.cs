using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EstadoPropuesta",
                table: "SolicitudesCambioDocumentoControl",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "DocumentosControl",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesCambioDocumentoControl_EstadoPropuesta",
                table: "SolicitudesCambioDocumentoControl",
                column: "EstadoPropuesta");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosControl_Estado",
                table: "DocumentosControl",
                column: "Estado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SolicitudesCambioDocumentoControl_EstadoPropuesta",
                table: "SolicitudesCambioDocumentoControl");

            migrationBuilder.DropIndex(
                name: "IX_DocumentosControl_Estado",
                table: "DocumentosControl");

            migrationBuilder.AlterColumn<string>(
                name: "EstadoPropuesta",
                table: "SolicitudesCambioDocumentoControl",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "DocumentosControl",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
