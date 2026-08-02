using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiTurno.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCobroAutomaticoPausadoToSuscripcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CobroAutomaticoPausado",
                table: "Suscripciones",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CobroAutomaticoPausado",
                table: "Suscripciones");
        }
    }
}
