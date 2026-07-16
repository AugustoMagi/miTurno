using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiTurno.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameCredencialesOAuthToAlias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CredencialesOAuth",
                table: "ConfiguracionesPago",
                newName: "Alias");

            migrationBuilder.AlterColumn<string>(
                name: "Alias",
                table: "ConfiguracionesPago",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Alias",
                table: "ConfiguracionesPago",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.RenameColumn(
                name: "Alias",
                table: "ConfiguracionesPago",
                newName: "CredencialesOAuth");
        }
    }
}
