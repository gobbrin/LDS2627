using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusCarpool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class I110_EstadoViagemConcluidaSemAcento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DC-32: o controller gravava "Concluída" (com acento); o modelo de dados v06
            // e o enum EstadoViagem usam "Concluida". Normaliza os dados antes do CHECK.
            migrationBuilder.Sql("UPDATE [Viagens] SET [Estado] = N'Concluida' WHERE [Estado] = N'Concluída';");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viagens_Estado",
                table: "Viagens",
                sql: "[Estado] IN ('Ativa', 'Indisponibilizada', 'Concluida')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Viagens_Estado",
                table: "Viagens");
        }
    }
}
