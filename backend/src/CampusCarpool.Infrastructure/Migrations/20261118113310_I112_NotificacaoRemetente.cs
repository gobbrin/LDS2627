using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusCarpool.Infrastructure.Migrations
{
    /// <summary>
    /// DEF-BE-053: a notificação <c>NovaMensagem</c> passa a guardar o remetente (<c>RemetenteId</c>), para o cliente
    /// abrir a conversa a partir dela.
    /// </summary>
    /// <remarks>
    /// Coluna anulável: os outros tipos não têm remetente, e as notificações já gravadas ficam com <c>NULL</c> (o texto
    /// continua com o nome). Chave estrangeira para <c>Utilizadores</c> sem cascata, como as outras chaves para o
    /// utilizador; o índice é o que o EF cria para cada chave estrangeira.
    /// </remarks>
    public partial class I112_NotificacaoRemetente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RemetenteId",
                table: "Notificacoes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_RemetenteId",
                table: "Notificacoes",
                column: "RemetenteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notificacoes_Utilizadores_RemetenteId",
                table: "Notificacoes",
                column: "RemetenteId",
                principalTable: "Utilizadores",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notificacoes_Utilizadores_RemetenteId",
                table: "Notificacoes");

            migrationBuilder.DropIndex(
                name: "IX_Notificacoes_RemetenteId",
                table: "Notificacoes");

            migrationBuilder.DropColumn(
                name: "RemetenteId",
                table: "Notificacoes");
        }
    }
}
