using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiTurno.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedSysAdmin : Migration
    {
        private static readonly Guid SeedAdminId = new("b6f1f7d1-6e2a-4b8a-9b7e-2b6f0f6c1a01");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SysAdmins",
                columns: new[] { "Id", "Nombre", "Email", "PasswordHash", "Activo", "FechaCreacion", "FechaActualizacion" },
                values: new object[]
                {
                    SeedAdminId,
                    "Admin",
                    "admin@miturno.com",
                    "$2a$12$w2S6QeI2y/8a3j/5mVNSp.kVvSHqrWbEYDxjjDnUU9TQE6EyWDXRa",
                    true,
                    new DateTime(2026, 7, 22, 0, 0, 0, DateTimeKind.Utc),
                    null
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SysAdmins",
                keyColumn: "Id",
                keyValue: SeedAdminId);
        }
    }
}
