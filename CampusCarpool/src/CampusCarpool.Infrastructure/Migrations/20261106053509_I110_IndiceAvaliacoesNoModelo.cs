using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusCarpool.Infrastructure.Migrations
{
    /// <summary>Alinha o modelo com a I048 (IN-01, DEF-BE-025), que trocou o índice triplo das avaliações pelo do par.</summary>
    /// <remarks>
    /// Escrita à mão: nas BD migradas o índice triplo já não existe e o do par já existe, por isso um <c>migrations add</c>
    /// falharia ao tentar apagar o primeiro. Só faltam os índices das chaves estrangeiras: o do par já cobre
    /// <c>AvaliadorId</c> e, sem o triplo, <c>ViagemId</c> ficou sem índice.
    /// </remarks>
    public partial class I110_IndiceAvaliacoesNoModelo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Avaliacoes_AvaliadorId",
                table: "Avaliacoes");

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacoes_ViagemId",
                table: "Avaliacoes",
                column: "ViagemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Avaliacoes_ViagemId",
                table: "Avaliacoes");

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacoes_AvaliadorId",
                table: "Avaliacoes",
                column: "AvaliadorId");
        }
    }
}
