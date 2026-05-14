using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoArchivoCertificado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoArchivoCertificado",
                table: "Capacitaciones",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoArchivoCertificado",
                table: "Capacitaciones");
        }
    }
}
