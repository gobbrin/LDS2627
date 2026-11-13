using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusCarpool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class I110_ParticipacaoAceiteEm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AceiteEm",
                table: "Participacoes",
                type: "datetime2",
                nullable: true);

            // As participações já aceites não guardaram a data da aceitação; usa-se a data
            // do pedido como aproximação, para que continuem no histórico se a viagem for
            // indisponibilizada (RF-F009-08, P-25).
            // EXEC: num script SQL, a coluna acabada de criar só é visível noutro lote.
            migrationBuilder.Sql("EXEC(N'UPDATE [Participacoes] SET [AceiteEm] = [CriadoEm] WHERE [Estado] = N''Aceite'';');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AceiteEm",
                table: "Participacoes");
        }
    }
}
