using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiTurno.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBloqueoFechaUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BloqueosFecha_RecursoId",
                table: "BloqueosFecha");

            migrationBuilder.CreateIndex(
                name: "IX_BloqueosFecha_RecursoId_Fecha",
                table: "BloqueosFecha",
                columns: new[] { "RecursoId", "Fecha" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BloqueosFecha_RecursoId_Fecha",
                table: "BloqueosFecha");

            migrationBuilder.CreateIndex(
                name: "IX_BloqueosFecha_RecursoId",
                table: "BloqueosFecha",
                column: "RecursoId");
        }
    }
}
