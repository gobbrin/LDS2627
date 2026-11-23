using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusCarpool.Infrastructure.Migrations
{
    /// <summary>
    /// Migration única da I112: retira os campos sem uso <c>Mensagens.ViagemId</c> (BE-22, DEF-BE-051) e
    /// <c>Avisos.CondutorId</c> (o autor é sempre o condutor da viagem; BE-33, DEF-BE-050) e cria o índice da conversa
    /// <c>(RemetenteId, DestinatarioId, EnviadoEm)</c> (BE-13, BE-33), que substitui o índice só de <c>RemetenteId</c>.
    /// </summary>
    public partial class I112_IndiceConversaECamposSemUso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // As chaves estrangeiras e os índices saem antes das colunas que usam.
            migrationBuilder.DropForeignKey(
                name: "FK_Avisos_Utilizadores_CondutorId",
                table: "Avisos");

            migrationBuilder.DropForeignKey(
                name: "FK_Mensagens_Viagens_ViagemId",
                table: "Mensagens");

            migrationBuilder.DropIndex(
                name: "IX_Mensagens_RemetenteId",
                table: "Mensagens");

            migrationBuilder.DropIndex(
                name: "IX_Mensagens_ViagemId",
                table: "Mensagens");

            migrationBuilder.DropIndex(
                name: "IX_Avisos_CondutorId",
                table: "Avisos");

            migrationBuilder.DropColumn(
                name: "ViagemId",
                table: "Mensagens");

            migrationBuilder.DropColumn(
                name: "CondutorId",
                table: "Avisos");

            migrationBuilder.CreateIndex(
                name: "IX_Mensagens_RemetenteId_DestinatarioId_EnviadoEm",
                table: "Mensagens",
                columns: new[] { "RemetenteId", "DestinatarioId", "EnviadoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Mensagens_RemetenteId_DestinatarioId_EnviadoEm",
                table: "Mensagens");

            migrationBuilder.AddColumn<Guid>(
                name: "ViagemId",
                table: "Mensagens",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CondutorId",
                table: "Avisos",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Repõe o autor de cada aviso a partir do condutor da viagem, para a chave estrangeira voltar a valer.
            // Dentro de EXEC: num script único, o lote seria compilado antes de a coluna existir.
            migrationBuilder.Sql("EXEC(N'UPDATE [Avisos] SET [CondutorId] = (SELECT [V].[CondutorId] FROM [Viagens] AS [V] WHERE [V].[Id] = [Avisos].[ViagemId]);');");

            migrationBuilder.CreateIndex(
                name: "IX_Mensagens_RemetenteId",
                table: "Mensagens",
                column: "RemetenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensagens_ViagemId",
                table: "Mensagens",
                column: "ViagemId");

            migrationBuilder.CreateIndex(
                name: "IX_Avisos_CondutorId",
                table: "Avisos",
                column: "CondutorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Avisos_Utilizadores_CondutorId",
                table: "Avisos",
                column: "CondutorId",
                principalTable: "Utilizadores",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Mensagens_Viagens_ViagemId",
                table: "Mensagens",
                column: "ViagemId",
                principalTable: "Viagens",
                principalColumn: "Id");
        }
    }
}
