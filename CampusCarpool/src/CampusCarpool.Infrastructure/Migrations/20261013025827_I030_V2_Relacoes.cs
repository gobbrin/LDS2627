using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusCarpool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class I030_V2_Relacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_UtilizadorId",
                table: "Notificacoes",
                column: "UtilizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_ViagemId",
                table: "Notificacoes",
                column: "ViagemId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensagens_DestinatarioId",
                table: "Mensagens",
                column: "DestinatarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensagens_RemetenteId",
                table: "Mensagens",
                column: "RemetenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensagens_ViagemId",
                table: "Mensagens",
                column: "ViagemId");

            migrationBuilder.CreateIndex(
                name: "IX_LigacoesExternas_UtilizadorId",
                table: "LigacoesExternas",
                column: "UtilizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Dispositivos_UtilizadorId",
                table: "Dispositivos",
                column: "UtilizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfirmacoesConta_UtilizadorId",
                table: "ConfirmacoesConta",
                column: "UtilizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Avisos_CondutorId",
                table: "Avisos",
                column: "CondutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Avisos_ViagemId",
                table: "Avisos",
                column: "ViagemId");

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacoes_AvaliadoId",
                table: "Avaliacoes",
                column: "AvaliadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Avaliacoes_AvaliadorId",
                table: "Avaliacoes",
                column: "AvaliadorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Avaliacoes_Utilizadores_AvaliadoId",
                table: "Avaliacoes",
                column: "AvaliadoId",
                principalTable: "Utilizadores",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Avaliacoes_Utilizadores_AvaliadorId",
                table: "Avaliacoes",
                column: "AvaliadorId",
                principalTable: "Utilizadores",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Avaliacoes_Viagens_ViagemId",
                table: "Avaliacoes",
                column: "ViagemId",
                principalTable: "Viagens",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Avisos_Utilizadores_CondutorId",
                table: "Avisos",
                column: "CondutorId",
                principalTable: "Utilizadores",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Avisos_Viagens_ViagemId",
                table: "Avisos",
                column: "ViagemId",
                principalTable: "Viagens",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ConfirmacoesConta_Utilizadores_UtilizadorId",
                table: "ConfirmacoesConta",
                column: "UtilizadorId",
                principalTable: "Utilizadores",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Dispositivos_Utilizadores_UtilizadorId",
                table: "Dispositivos",
                column: "UtilizadorId",
                principalTable: "Utilizadores",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LigacoesExternas_Utilizadores_UtilizadorId",
                table: "LigacoesExternas",
                column: "UtilizadorId",
                principalTable: "Utilizadores",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Mensagens_Utilizadores_DestinatarioId",
                table: "Mensagens",
                column: "DestinatarioId",
                principalTable: "Utilizadores",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Mensagens_Utilizadores_RemetenteId",
                table: "Mensagens",
                column: "RemetenteId",
                principalTable: "Utilizadores",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Mensagens_Viagens_ViagemId",
                table: "Mensagens",
                column: "ViagemId",
                principalTable: "Viagens",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notificacoes_Utilizadores_UtilizadorId",
                table: "Notificacoes",
                column: "UtilizadorId",
                principalTable: "Utilizadores",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notificacoes_Viagens_ViagemId",
                table: "Notificacoes",
                column: "ViagemId",
                principalTable: "Viagens",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Avaliacoes_Utilizadores_AvaliadoId",
                table: "Avaliacoes");

            migrationBuilder.DropForeignKey(
                name: "FK_Avaliacoes_Utilizadores_AvaliadorId",
                table: "Avaliacoes");

            migrationBuilder.DropForeignKey(
                name: "FK_Avaliacoes_Viagens_ViagemId",
                table: "Avaliacoes");

            migrationBuilder.DropForeignKey(
                name: "FK_Avisos_Utilizadores_CondutorId",
                table: "Avisos");

            migrationBuilder.DropForeignKey(
                name: "FK_Avisos_Viagens_ViagemId",
                table: "Avisos");

            migrationBuilder.DropForeignKey(
                name: "FK_ConfirmacoesConta_Utilizadores_UtilizadorId",
                table: "ConfirmacoesConta");

            migrationBuilder.DropForeignKey(
                name: "FK_Dispositivos_Utilizadores_UtilizadorId",
                table: "Dispositivos");

            migrationBuilder.DropForeignKey(
                name: "FK_LigacoesExternas_Utilizadores_UtilizadorId",
                table: "LigacoesExternas");

            migrationBuilder.DropForeignKey(
                name: "FK_Mensagens_Utilizadores_DestinatarioId",
                table: "Mensagens");

            migrationBuilder.DropForeignKey(
                name: "FK_Mensagens_Utilizadores_RemetenteId",
                table: "Mensagens");

            migrationBuilder.DropForeignKey(
                name: "FK_Mensagens_Viagens_ViagemId",
                table: "Mensagens");

            migrationBuilder.DropForeignKey(
                name: "FK_Notificacoes_Utilizadores_UtilizadorId",
                table: "Notificacoes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notificacoes_Viagens_ViagemId",
                table: "Notificacoes");

            migrationBuilder.DropIndex(
                name: "IX_Notificacoes_UtilizadorId",
                table: "Notificacoes");

            migrationBuilder.DropIndex(
                name: "IX_Notificacoes_ViagemId",
                table: "Notificacoes");

            migrationBuilder.DropIndex(
                name: "IX_Mensagens_DestinatarioId",
                table: "Mensagens");

            migrationBuilder.DropIndex(
                name: "IX_Mensagens_RemetenteId",
                table: "Mensagens");

            migrationBuilder.DropIndex(
                name: "IX_Mensagens_ViagemId",
                table: "Mensagens");

            migrationBuilder.DropIndex(
                name: "IX_LigacoesExternas_UtilizadorId",
                table: "LigacoesExternas");

            migrationBuilder.DropIndex(
                name: "IX_Dispositivos_UtilizadorId",
                table: "Dispositivos");

            migrationBuilder.DropIndex(
                name: "IX_ConfirmacoesConta_UtilizadorId",
                table: "ConfirmacoesConta");

            migrationBuilder.DropIndex(
                name: "IX_Avisos_CondutorId",
                table: "Avisos");

            migrationBuilder.DropIndex(
                name: "IX_Avisos_ViagemId",
                table: "Avisos");

            migrationBuilder.DropIndex(
                name: "IX_Avaliacoes_AvaliadoId",
                table: "Avaliacoes");

            migrationBuilder.DropIndex(
                name: "IX_Avaliacoes_AvaliadorId",
                table: "Avaliacoes");
        }
    }
}
