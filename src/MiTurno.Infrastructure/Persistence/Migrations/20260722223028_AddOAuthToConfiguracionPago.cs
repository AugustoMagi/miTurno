using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiTurno.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthToConfiguracionPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AccessTokenExpiraEn",
                table: "ConfiguracionesPago",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "ConfiguracionesPago",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessTokenExpiraEn",
                table: "ConfiguracionesPago");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "ConfiguracionesPago");
        }
    }
}
