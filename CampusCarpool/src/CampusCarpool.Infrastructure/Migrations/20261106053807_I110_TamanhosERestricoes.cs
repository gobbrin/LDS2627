using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusCarpool.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class I110_TamanhosERestricoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server não altera uma coluna usada por um CHECK ou por um índice filtrado: saem antes e voltam depois.
            migrationBuilder.DropCheckConstraint(
                name: "CK_Viagens_Estado",
                table: "Viagens");

            migrationBuilder.DropIndex(
                name: "IX_Participacoes_ViagemId_PassageiroId",
                table: "Participacoes");

            // A API aceitava 250 caracteres nestes campos; o modelo prevê 200.
            migrationBuilder.Sql("""
                UPDATE [Viagens] SET [Origem] = LEFT([Origem], 200) WHERE LEN([Origem]) > 200;
                UPDATE [Viagens] SET [Destino] = LEFT([Destino], 200) WHERE LEN([Destino]) > 200;
                UPDATE [Participacoes] SET [Recolha] = LEFT([Recolha], 200) WHERE LEN([Recolha]) > 200;
                UPDATE [Participacoes] SET [Destino] = LEFT([Destino], 200) WHERE LEN([Destino]) > 200;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "OrigemPesquisa",
                table: "Viagens",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Origem",
                table: "Viagens",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Viagens",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DestinoPesquisa",
                table: "Viagens",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Destino",
                table: "Viagens",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Utilizadores",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Instituicao",
                table: "Utilizadores",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Curso",
                table: "Utilizadores",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Recolha",
                table: "Participacoes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Participacoes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Destino",
                table: "Participacoes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Titulo",
                table: "Notificacoes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Tipo",
                table: "Notificacoes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Conteudo",
                table: "Notificacoes",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Conteudo",
                table: "Mensagens",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "LigacoesExternas",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Descricao",
                table: "LigacoesExternas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "Dispositivos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "ConfirmacoesConta",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            // A I110_PesquisaSemAcentos deixou vazias as colunas de pesquisa das viagens já existentes. A conversão para a
            // página de código 1253 retira os acentos do português; um símbolo fora dela pode ficar diferente de
            // TextoPesquisa.Normalizar, e o valor certo volta quando a viagem for gravada.
            migrationBuilder.Sql("""
                EXEC(N'UPDATE [Viagens] SET
                    [OrigemPesquisa] = UPPER(CAST([Origem] COLLATE SQL_Latin1_General_CP1253_CI_AI AS varchar(200))),
                    [DestinoPesquisa] = UPPER(CAST([Destino] COLLATE SQL_Latin1_General_CP1253_CI_AI AS varchar(200)))
                    WHERE [OrigemPesquisa] = N'''' OR [DestinoPesquisa] = N'''';');
                """);

            // DC-40: os títulos guardados eram o nome do tipo.
            migrationBuilder.Sql("""
                UPDATE [Notificacoes] SET [Titulo] = CASE [Titulo]
                    WHEN N'PedidoRecebido' THEN N'Pedido recebido'
                    WHEN N'PedidoAceite' THEN N'Pedido aceite'
                    WHEN N'PedidoRejeitado' THEN N'Pedido rejeitado'
                    WHEN N'AlteracaoViagem' THEN N'Alteração da viagem'
                    WHEN N'NovaMensagem' THEN N'Nova mensagem'
                    ELSE [Titulo] END;
                """);

            // As restrições novas valem para o que se grave daqui em diante (WITH NOCHECK): uma linha antiga fora delas não
            // pode impedir a migration.
            migrationBuilder.AddCheckConstraint(
                name: "CK_Viagens_Estado",
                table: "Viagens",
                sql: "[Estado] IN ('Ativa', 'Indisponibilizada', 'Concluida')");

            migrationBuilder.CreateIndex(
                name: "IX_Participacoes_ViagemId_PassageiroId",
                table: "Participacoes",
                columns: new[] { "ViagemId", "PassageiroId" },
                unique: true,
                filter: "[Estado] IN ('Pendente', 'Aceite')");

            migrationBuilder.CreateIndex(
                name: "IX_Viagens_Estado_PartidaEm",
                table: "Viagens",
                columns: new[] { "Estado", "PartidaEm" });

            migrationBuilder.Sql("ALTER TABLE [Viagens] WITH NOCHECK ADD CONSTRAINT [CK_Viagens_Locais] CHECK (NOT ([OrigemLatitude] = [DestinoLatitude] AND [OrigemLongitude] = [DestinoLongitude]));");

            migrationBuilder.Sql("ALTER TABLE [Viagens] WITH NOCHECK ADD CONSTRAINT [CK_Viagens_Lugares] CHECK ([LugaresOferecidos] BETWEEN 1 AND 8 AND [LugaresDisponiveis] BETWEEN 0 AND [LugaresOferecidos]);");

            migrationBuilder.Sql("ALTER TABLE [Viagens] WITH NOCHECK ADD CONSTRAINT [CK_Viagens_Rota] CHECK ([RotaPontos] IS NULL OR ISJSON([RotaPontos]) = 1);");

            migrationBuilder.Sql("ALTER TABLE [Participacoes] WITH NOCHECK ADD CONSTRAINT [CK_Participacoes_Estado] CHECK ([Estado] IN ('Pendente', 'Aceite', 'Rejeitada', 'Cancelada'));");

            migrationBuilder.Sql("ALTER TABLE [Participacoes] WITH NOCHECK ADD CONSTRAINT [CK_Participacoes_Locais] CHECK (NOT ([RecolhaLatitude] = [DestinoLatitude] AND [RecolhaLongitude] = [DestinoLongitude]));");

            migrationBuilder.Sql("ALTER TABLE [Notificacoes] WITH NOCHECK ADD CONSTRAINT [CK_Notificacoes_Tipo] CHECK ([Tipo] IN ('PedidoRecebido', 'PedidoAceite', 'PedidoRejeitado', 'AlteracaoViagem', 'Aviso', 'NovaMensagem'));");

            migrationBuilder.Sql("ALTER TABLE [Mensagens] WITH NOCHECK ADD CONSTRAINT [CK_Mensagens_Conteudo] CHECK ([Conteudo] <> '');");

            migrationBuilder.Sql("ALTER TABLE [Mensagens] WITH NOCHECK ADD CONSTRAINT [CK_Mensagens_Distintos] CHECK ([RemetenteId] <> [DestinatarioId]);");

            migrationBuilder.Sql("ALTER TABLE [Avaliacoes] WITH NOCHECK ADD CONSTRAINT [CK_Avaliacoes_Classificacao] CHECK ([Classificacao] BETWEEN 1 AND 5);");

            migrationBuilder.Sql("ALTER TABLE [Avaliacoes] WITH NOCHECK ADD CONSTRAINT [CK_Avaliacoes_Distintos] CHECK ([AvaliadorId] <> [AvaliadoId]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // SQL Server não altera uma coluna usada por um CHECK ou por um índice filtrado: saem antes e voltam depois.
            migrationBuilder.DropCheckConstraint(
                name: "CK_Viagens_Estado",
                table: "Viagens");

            migrationBuilder.DropIndex(
                name: "IX_Participacoes_ViagemId_PassageiroId",
                table: "Participacoes");

            migrationBuilder.DropIndex(
                name: "IX_Viagens_Estado_PartidaEm",
                table: "Viagens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viagens_Locais",
                table: "Viagens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viagens_Lugares",
                table: "Viagens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viagens_Rota",
                table: "Viagens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Participacoes_Estado",
                table: "Participacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Participacoes_Locais",
                table: "Participacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notificacoes_Tipo",
                table: "Notificacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Mensagens_Conteudo",
                table: "Mensagens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Mensagens_Distintos",
                table: "Mensagens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Avaliacoes_Classificacao",
                table: "Avaliacoes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Avaliacoes_Distintos",
                table: "Avaliacoes");

            migrationBuilder.AlterColumn<string>(
                name: "OrigemPesquisa",
                table: "Viagens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400);

            migrationBuilder.AlterColumn<string>(
                name: "Origem",
                table: "Viagens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Viagens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "DestinoPesquisa",
                table: "Viagens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400);

            migrationBuilder.AlterColumn<string>(
                name: "Destino",
                table: "Viagens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Utilizadores",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Instituicao",
                table: "Utilizadores",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Curso",
                table: "Utilizadores",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Recolha",
                table: "Participacoes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Participacoes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Destino",
                table: "Participacoes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Titulo",
                table: "Notificacoes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Tipo",
                table: "Notificacoes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Conteudo",
                table: "Notificacoes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "Conteudo",
                table: "Mensagens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "LigacoesExternas",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2048)",
                oldMaxLength: 2048);

            migrationBuilder.AlterColumn<string>(
                name: "Descricao",
                table: "LigacoesExternas",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "Dispositivos",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "ConfirmacoesConta",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viagens_Estado",
                table: "Viagens",
                sql: "[Estado] IN ('Ativa', 'Indisponibilizada', 'Concluida')");

            migrationBuilder.CreateIndex(
                name: "IX_Participacoes_ViagemId_PassageiroId",
                table: "Participacoes",
                columns: new[] { "ViagemId", "PassageiroId" },
                unique: true,
                filter: "[Estado] IN ('Pendente', 'Aceite')");
        }
    }
}
