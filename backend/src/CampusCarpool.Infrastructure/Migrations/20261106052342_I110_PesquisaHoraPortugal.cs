using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusCarpool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class I110_PesquisaHoraPortugal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PartidaMinutoDoDia",
                table: "Viagens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // "GMT Standard Time" é o nome Windows do fuso de Lisboa e de Londres, com a mesma hora de verão.
            // EXEC: num script único, o lote seria compilado antes de a coluna existir.
            migrationBuilder.Sql("""
                EXEC(N'UPDATE [Viagens] SET [PartidaMinutoDoDia] =
                    DATEPART(HOUR, [PartidaEm] AT TIME ZONE ''UTC'' AT TIME ZONE ''GMT Standard Time'') * 60 +
                    DATEPART(MINUTE, [PartidaEm] AT TIME ZONE ''UTC'' AT TIME ZONE ''GMT Standard Time'');');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartidaMinutoDoDia",
                table: "Viagens");
        }
    }
}
