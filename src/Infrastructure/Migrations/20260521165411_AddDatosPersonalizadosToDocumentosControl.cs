using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDatosPersonalizadosToDocumentosControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DatosPersonalizados",
                table: "DocumentosControl",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DatosPersonalizados",
                table: "DocumentosControl");
        }
    }
}
