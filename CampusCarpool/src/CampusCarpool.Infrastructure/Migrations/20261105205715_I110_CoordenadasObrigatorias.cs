using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusCarpool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class I110_CoordenadasObrigatorias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "OrigemLongitude",
                table: "Viagens",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "OrigemLatitude",
                table: "Viagens",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DestinoLongitude",
                table: "Viagens",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DestinoLatitude",
                table: "Viagens",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DestinoLatitude",
                table: "Participacoes",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DestinoLongitude",
                table: "Participacoes",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RecolhaLatitude",
                table: "Participacoes",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RecolhaLongitude",
                table: "Participacoes",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestinoLatitude",
                table: "Participacoes");

            migrationBuilder.DropColumn(
                name: "DestinoLongitude",
                table: "Participacoes");

            migrationBuilder.DropColumn(
                name: "RecolhaLatitude",
                table: "Participacoes");

            migrationBuilder.DropColumn(
                name: "RecolhaLongitude",
                table: "Participacoes");

            migrationBuilder.AlterColumn<decimal>(
                name: "OrigemLongitude",
                table: "Viagens",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6);

            migrationBuilder.AlterColumn<decimal>(
                name: "OrigemLatitude",
                table: "Viagens",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6);

            migrationBuilder.AlterColumn<decimal>(
                name: "DestinoLongitude",
                table: "Viagens",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6);

            migrationBuilder.AlterColumn<decimal>(
                name: "DestinoLatitude",
                table: "Viagens",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6);
        }
    }
}
