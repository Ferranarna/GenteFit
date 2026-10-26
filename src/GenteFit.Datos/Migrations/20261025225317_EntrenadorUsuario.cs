using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenteFit.Datos.Migrations
{
    /// <inheritdoc />
    public partial class EntrenadorUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Entrenadores");

            migrationBuilder.DropColumn(
                name: "Apellido1",
                table: "Entrenadores");

            migrationBuilder.DropColumn(
                name: "Apellido2",
                table: "Entrenadores");

            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "Entrenadores");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Entrenadores");

            migrationBuilder.AddForeignKey(
                name: "FK_Entrenadores_Usuarios_EntrenadorId",
                table: "Entrenadores",
                column: "EntrenadorId",
                principalTable: "Usuarios",
                principalColumn: "UsuarioId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Entrenadores_Usuarios_EntrenadorId",
                table: "Entrenadores");

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Entrenadores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Apellido1",
                table: "Entrenadores",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Apellido2",
                table: "Entrenadores",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "Entrenadores",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Telefono",
                table: "Entrenadores",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
