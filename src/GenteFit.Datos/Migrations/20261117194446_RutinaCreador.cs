using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenteFit.Datos.Migrations
{
    /// <inheritdoc />
    public partial class RutinaCreador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EntrenadorId",
                table: "Rutinas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Rutinas",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Rutinas_EntrenadorId",
                table: "Rutinas",
                column: "EntrenadorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rutinas_Entrenadores_EntrenadorId",
                table: "Rutinas",
                column: "EntrenadorId",
                principalTable: "Entrenadores",
                principalColumn: "EntrenadorId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rutinas_Entrenadores_EntrenadorId",
                table: "Rutinas");

            migrationBuilder.DropIndex(
                name: "IX_Rutinas_EntrenadorId",
                table: "Rutinas");

            migrationBuilder.DropColumn(
                name: "EntrenadorId",
                table: "Rutinas");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Rutinas");
        }
    }
}
