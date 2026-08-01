using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiTurno.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHorarioABloqueoFecha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BloqueosFecha_RecursoId_Fecha",
                table: "BloqueosFecha");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "HoraFin",
                table: "BloqueosFecha",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "HoraInicio",
                table: "BloqueosFecha",
                type: "time",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BloqueosFecha_RecursoId_Fecha",
                table: "BloqueosFecha",
                columns: new[] { "RecursoId", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BloqueosFecha_RecursoId_Fecha",
                table: "BloqueosFecha");

            migrationBuilder.DropColumn(
                name: "HoraFin",
                table: "BloqueosFecha");

            migrationBuilder.DropColumn(
                name: "HoraInicio",
                table: "BloqueosFecha");

            migrationBuilder.CreateIndex(
                name: "IX_BloqueosFecha_RecursoId_Fecha",
                table: "BloqueosFecha",
                columns: new[] { "RecursoId", "Fecha" },
                unique: true);
        }
    }
}
