using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusCarpool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class I038_IndicePedidosAtivos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Participacoes_ViagemId",
                table: "Participacoes");

            migrationBuilder.CreateIndex(
                name: "IX_Participacoes_ViagemId_PassageiroId",
                table: "Participacoes",
                columns: new[] { "ViagemId", "PassageiroId" },
                unique: true,
                filter: "[Estado] IN ('Pendente', 'Aceite')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Participacoes_ViagemId_PassageiroId",
                table: "Participacoes");

            migrationBuilder.CreateIndex(
                name: "IX_Participacoes_ViagemId",
                table: "Participacoes",
                column: "ViagemId");
        }
    }
}
