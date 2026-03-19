using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJefeToArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "JefeId",
                table: "Areas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Areas_JefeId",
                table: "Areas",
                column: "JefeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_Colaboradores_JefeId",
                table: "Areas",
                column: "JefeId",
                principalTable: "Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_Colaboradores_JefeId",
                table: "Areas");

            migrationBuilder.DropIndex(
                name: "IX_Areas_JefeId",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "JefeId",
                table: "Areas");
        }
    }
}
