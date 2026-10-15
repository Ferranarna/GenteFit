using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenteFit.Datos.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Actividades",
                columns: table => new
                {
                    ActividadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreActividad = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Intensidad = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LimiteUsuarios = table.Column<int>(type: "int", nullable: false),
                    Especialidad = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UrlImagen = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actividades", x => x.ActividadId);
                });

            migrationBuilder.CreateTable(
                name: "Ejercicios",
                columns: table => new
                {
                    EjercicioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreEjercicio = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    GrupoMuscular = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UrlImagen = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ejercicios", x => x.EjercicioId);
                });

            migrationBuilder.CreateTable(
                name: "Entrenadores",
                columns: table => new
                {
                    EntrenadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Apellido1 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Apellido2 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Telefono = table.Column<int>(type: "int", nullable: false),
                    Especialidad = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entrenadores", x => x.EntrenadorId);
                });

            migrationBuilder.CreateTable(
                name: "Salas",
                columns: table => new
                {
                    SalaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreSala = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Ubicacion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salas", x => x.SalaId);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Apellido1 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Apellido2 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Dni = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Contrasena = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Telefono = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.UsuarioId);
                });

            migrationBuilder.CreateTable(
                name: "Sesiones",
                columns: table => new
                {
                    SesionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duracion = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ActividadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntrenadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sesiones", x => x.SesionId);
                    table.ForeignKey(
                        name: "FK_Sesiones_Actividades_ActividadId",
                        column: x => x.ActividadId,
                        principalTable: "Actividades",
                        principalColumn: "ActividadId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sesiones_Entrenadores_EntrenadorId",
                        column: x => x.EntrenadorId,
                        principalTable: "Entrenadores",
                        principalColumn: "EntrenadorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sesiones_Salas_SalaId",
                        column: x => x.SalaId,
                        principalTable: "Salas",
                        principalColumn: "SalaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Citas",
                columns: table => new
                {
                    CitaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaActividad = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duracion = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntrenadorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citas", x => x.CitaId);
                    table.ForeignKey(
                        name: "FK_Citas_Entrenadores_EntrenadorId",
                        column: x => x.EntrenadorId,
                        principalTable: "Entrenadores",
                        principalColumn: "EntrenadorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Citas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rutinas",
                columns: table => new
                {
                    RutinaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreRutina = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rutinas", x => x.RutinaId);
                    table.ForeignKey(
                        name: "FK_Rutinas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    ReservaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaReserva = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SesionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.ReservaId);
                    table.ForeignKey(
                        name: "FK_Reservas_Sesiones_SesionId",
                        column: x => x.SesionId,
                        principalTable: "Sesiones",
                        principalColumn: "SesionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesRutina",
                columns: table => new
                {
                    DetalleRutinaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Series = table.Column<int>(type: "int", nullable: false),
                    Repeticiones = table.Column<int>(type: "int", nullable: false),
                    RutinaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EjercicioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesRutina", x => x.DetalleRutinaId);
                    table.ForeignKey(
                        name: "FK_DetallesRutina_Ejercicios_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "Ejercicios",
                        principalColumn: "EjercicioId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesRutina_Rutinas_RutinaId",
                        column: x => x.RutinaId,
                        principalTable: "Rutinas",
                        principalColumn: "RutinaId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Citas_EntrenadorId",
                table: "Citas",
                column: "EntrenadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_UsuarioId",
                table: "Citas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesRutina_EjercicioId",
                table: "DetallesRutina",
                column: "EjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesRutina_RutinaId",
                table: "DetallesRutina",
                column: "RutinaId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_SesionId",
                table: "Reservas",
                column: "SesionId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_UsuarioId_SesionId",
                table: "Reservas",
                columns: new[] { "UsuarioId", "SesionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rutinas_UsuarioId",
                table: "Rutinas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Sesiones_ActividadId",
                table: "Sesiones",
                column: "ActividadId");

            migrationBuilder.CreateIndex(
                name: "IX_Sesiones_EntrenadorId",
                table: "Sesiones",
                column: "EntrenadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Sesiones_SalaId",
                table: "Sesiones",
                column: "SalaId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Dni",
                table: "Usuarios",
                column: "Dni",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Citas");

            migrationBuilder.DropTable(
                name: "DetallesRutina");

            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "Ejercicios");

            migrationBuilder.DropTable(
                name: "Rutinas");

            migrationBuilder.DropTable(
                name: "Sesiones");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Actividades");

            migrationBuilder.DropTable(
                name: "Entrenadores");

            migrationBuilder.DropTable(
                name: "Salas");
        }
    }
}
