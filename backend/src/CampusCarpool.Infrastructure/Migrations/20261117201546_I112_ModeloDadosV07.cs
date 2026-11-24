using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusCarpool.Infrastructure.Migrations
{
    /// <summary>
    /// Modelo de dados v07 (BE-33, DEF-BE-052): as restrições <c>CHECK</c>, os tamanhos e os índices que faltavam.
    /// </summary>
    /// <remarks>
    /// <para>9 restrições novas: nome e aviso sem texto vazio, URL http/https, coordenadas e medidas da viagem, um ponto
    /// por pedido, coordenadas de cada ponto e data de aceitação. A 10.ª da v07 (<c>CK_ConfirmacoesConta_Prazo</c>) não
    /// entra: a tabela não tem <c>CriadoEm</c> e invalidar uma ligação põe <c>ExpiraEm</c> no momento atual.</para>
    /// <para>5 tamanhos: estado da viagem 20, tipo da notificação 30, título 150, token do dispositivo 450, hash da
    /// confirmação 450.</para>
    /// <para>5 índices (o 6.º, o da conversa, veio na <c>I112_IndiceConversaECamposSemUso</c>): pesquisa com colunas
    /// incluídas, pedidos por viagem e estado, mensagens recebidas, avisos por viagem e data, notificações por data
    /// decrescente.</para>
    /// <para>As restrições valem para o que se grave daqui em diante (<c>WITH NOCHECK</c>): uma linha antiga fora delas,
    /// como um pedido sem pontos do m3, não impede a migration. O SQL Server só volta a verificar uma restrição quando a
    /// gravação mexe nas colunas dela.</para>
    /// </remarks>
    public partial class I112_ModeloDadosV07 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server não altera uma coluna usada por um CHECK ou por um índice: saem antes e voltam depois.
            migrationBuilder.DropCheckConstraint(
                name: "CK_Viagens_Estado",
                table: "Viagens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notificacoes_Tipo",
                table: "Notificacoes");

            // Um token FCM real tem cerca de 160 caracteres; um maior que a coluna nova não é válido e fica inativo.
            migrationBuilder.Sql("""
                UPDATE [Dispositivos] SET [Token] = LEFT([Token], 450), [Ativo] = 0 WHERE LEN([Token]) > 450;
                """);

            // Uma participação aceite antes da I110 pode não ter a data da aceitação: usa-se a do pedido.
            migrationBuilder.Sql("""
                UPDATE [Participacoes] SET [AceiteEm] = [CriadoEm] WHERE [Estado] = 'Aceite' AND [AceiteEm] IS NULL;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Viagens_Estado_PartidaEm",
                table: "Viagens");

            migrationBuilder.DropIndex(
                name: "IX_Notificacoes_UtilizadorId",
                table: "Notificacoes");

            migrationBuilder.DropIndex(
                name: "IX_Mensagens_DestinatarioId",
                table: "Mensagens");

            migrationBuilder.DropIndex(
                name: "IX_Avisos_ViagemId",
                table: "Avisos");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Viagens",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Titulo",
                table: "Notificacoes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Tipo",
                table: "Notificacoes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "Dispositivos",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "ConfirmacoesConta",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viagens_Estado",
                table: "Viagens",
                sql: "[Estado] IN ('Ativa', 'Indisponibilizada', 'Concluida')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notificacoes_Tipo",
                table: "Notificacoes",
                sql: "[Tipo] IN ('PedidoRecebido', 'PedidoAceite', 'PedidoRejeitado', 'AlteracaoViagem', 'Aviso', 'NovaMensagem')");

            migrationBuilder.CreateIndex(
                name: "IX_Viagens_Estado_PartidaEm",
                table: "Viagens",
                columns: new[] { "Estado", "PartidaEm" })
                .Annotation("SqlServer:Include", new[] { "OrigemPesquisa", "DestinoPesquisa", "LugaresDisponiveis" });

            migrationBuilder.Sql("ALTER TABLE [Viagens] WITH NOCHECK ADD CONSTRAINT [CK_Viagens_Coordenadas] CHECK ([OrigemLatitude] BETWEEN -90 AND 90 AND [DestinoLatitude] BETWEEN -90 AND 90 AND [OrigemLongitude] BETWEEN -180 AND 180 AND [DestinoLongitude] BETWEEN -180 AND 180);");

            migrationBuilder.Sql("ALTER TABLE [Viagens] WITH NOCHECK ADD CONSTRAINT [CK_Viagens_Medidas] CHECK (([DistanciaKm] IS NULL OR [DistanciaKm] > 0) AND ([DuracaoMinutos] IS NULL OR [DuracaoMinutos] > 0));");

            migrationBuilder.Sql("ALTER TABLE [Utilizadores] WITH NOCHECK ADD CONSTRAINT [CK_Utilizadores_Nome] CHECK (TRIM([Nome]) <> '');");

            migrationBuilder.CreateIndex(
                name: "IX_Participacoes_ViagemId_Estado",
                table: "Participacoes",
                columns: new[] { "ViagemId", "Estado" });

            migrationBuilder.Sql("ALTER TABLE [Participacoes] WITH NOCHECK ADD CONSTRAINT [CK_Participacoes_AceiteEm] CHECK ([Estado] <> 'Aceite' OR [AceiteEm] IS NOT NULL);");

            migrationBuilder.Sql("ALTER TABLE [Participacoes] WITH NOCHECK ADD CONSTRAINT [CK_Participacoes_CoordDestino] CHECK (([Destino] IS NULL AND [DestinoLatitude] IS NULL AND [DestinoLongitude] IS NULL) OR ([Destino] IS NOT NULL AND [DestinoLatitude] IS NOT NULL AND [DestinoLongitude] IS NOT NULL));");

            migrationBuilder.Sql("ALTER TABLE [Participacoes] WITH NOCHECK ADD CONSTRAINT [CK_Participacoes_CoordRecolha] CHECK (([Recolha] IS NULL AND [RecolhaLatitude] IS NULL AND [RecolhaLongitude] IS NULL) OR ([Recolha] IS NOT NULL AND [RecolhaLatitude] IS NOT NULL AND [RecolhaLongitude] IS NOT NULL));");

            migrationBuilder.Sql("ALTER TABLE [Participacoes] WITH NOCHECK ADD CONSTRAINT [CK_Participacoes_UmPonto] CHECK ([Recolha] IS NOT NULL OR [Destino] IS NOT NULL);");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_UtilizadorId_CriadaEm",
                table: "Notificacoes",
                columns: new[] { "UtilizadorId", "CriadaEm" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Mensagens_DestinatarioId_EnviadoEm",
                table: "Mensagens",
                columns: new[] { "DestinatarioId", "EnviadoEm" });

            migrationBuilder.Sql("ALTER TABLE [LigacoesExternas] WITH NOCHECK ADD CONSTRAINT [CK_LigacoesExternas_Url] CHECK ([Url] LIKE 'http://%' OR [Url] LIKE 'https://%');");

            migrationBuilder.CreateIndex(
                name: "IX_Avisos_ViagemId_CriadoEm",
                table: "Avisos",
                columns: new[] { "ViagemId", "CriadoEm" });

            migrationBuilder.Sql("ALTER TABLE [Avisos] WITH NOCHECK ADD CONSTRAINT [CK_Avisos_Conteudo] CHECK (TRIM([Conteudo]) <> '');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Viagens_Estado_PartidaEm",
                table: "Viagens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viagens_Estado",
                table: "Viagens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notificacoes_Tipo",
                table: "Notificacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viagens_Coordenadas",
                table: "Viagens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viagens_Medidas",
                table: "Viagens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Utilizadores_Nome",
                table: "Utilizadores");

            migrationBuilder.DropIndex(
                name: "IX_Participacoes_ViagemId_Estado",
                table: "Participacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Participacoes_AceiteEm",
                table: "Participacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Participacoes_CoordDestino",
                table: "Participacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Participacoes_CoordRecolha",
                table: "Participacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Participacoes_UmPonto",
                table: "Participacoes");

            migrationBuilder.DropIndex(
                name: "IX_Notificacoes_UtilizadorId_CriadaEm",
                table: "Notificacoes");

            migrationBuilder.DropIndex(
                name: "IX_Mensagens_DestinatarioId_EnviadoEm",
                table: "Mensagens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LigacoesExternas_Url",
                table: "LigacoesExternas");

            migrationBuilder.DropIndex(
                name: "IX_Avisos_ViagemId_CriadoEm",
                table: "Avisos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Avisos_Conteudo",
                table: "Avisos");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Viagens",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            // Um título com mais de 100 caracteres não cabe na coluna antiga.
            migrationBuilder.Sql("UPDATE [Notificacoes] SET [Titulo] = LEFT([Titulo], 100) WHERE LEN([Titulo]) > 100;");

            migrationBuilder.AlterColumn<string>(
                name: "Titulo",
                table: "Notificacoes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Tipo",
                table: "Notificacoes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "Dispositivos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "ConfirmacoesConta",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.Sql("ALTER TABLE [Viagens] WITH NOCHECK ADD CONSTRAINT [CK_Viagens_Estado] CHECK ([Estado] IN ('Ativa', 'Indisponibilizada', 'Concluida'));");

            migrationBuilder.Sql("ALTER TABLE [Notificacoes] WITH NOCHECK ADD CONSTRAINT [CK_Notificacoes_Tipo] CHECK ([Tipo] IN ('PedidoRecebido', 'PedidoAceite', 'PedidoRejeitado', 'AlteracaoViagem', 'Aviso', 'NovaMensagem'));");

            migrationBuilder.CreateIndex(
                name: "IX_Viagens_Estado_PartidaEm",
                table: "Viagens",
                columns: new[] { "Estado", "PartidaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_UtilizadorId",
                table: "Notificacoes",
                column: "UtilizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensagens_DestinatarioId",
                table: "Mensagens",
                column: "DestinatarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Avisos_ViagemId",
                table: "Avisos",
                column: "ViagemId");
        }
    }
}
