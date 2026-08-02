using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiTurno.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanPendienteToSuscripcion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoPreapprovalIdPendiente",
                table: "Suscripciones",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlanPendienteId",
                table: "Suscripciones",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MercadoPagoPreapprovalIdPendiente",
                table: "Suscripciones");

            migrationBuilder.DropColumn(
                name: "PlanPendienteId",
                table: "Suscripciones");
        }
    }
}
