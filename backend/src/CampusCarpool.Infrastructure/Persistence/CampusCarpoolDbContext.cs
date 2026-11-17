using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Persistence;

/// <summary>
/// Contexto EF Core da aplicação: uma tabela por entidade do Domain, em SQL Server (testes: SQLite em memória).
/// </summary>
/// <remarks>
/// Toda a configuração do modelo está em <see cref="OnModelCreating"/>.
/// O esquema real das BD de produção vem das migrations (<c>Migrations/</c>), aplicadas à
/// parte; os testes em SQLite usam <c>EnsureCreated</c>, que segue este modelo.
/// Todas as datas são UTC: <see cref="ConfigureConventions"/> aplica o <see cref="ConversorDataUtc"/>.
/// Os tamanhos, as restrições <c>CHECK</c> e os índices seguem o modelo de dados v07 (DC-36, BE-33, DEF-BE-052).
/// </remarks>
public sealed class CampusCarpoolDbContext(DbContextOptions<CampusCarpoolDbContext> options) : DbContext(options)
{
    /// <summary>Contas dos utilizadores.</summary>
    public DbSet<Utilizador> Utilizadores => Set<Utilizador>();
    /// <summary>Viagens oferecidas.</summary>
    public DbSet<Viagem> Viagens => Set<Viagem>();
    /// <summary>Pedidos de participação.</summary>
    public DbSet<Participacao> Participacoes => Set<Participacao>();
    /// <summary>Ligações a perfis externos.</summary>
    public DbSet<LigacaoExterna> LigacoesExternas => Set<LigacaoExterna>();
    /// <summary>Avaliações entre participantes.</summary>
    public DbSet<Avaliacao> Avaliacoes => Set<Avaliacao>();
    /// <summary>Mensagens individuais.</summary>
    public DbSet<Mensagem> Mensagens => Set<Mensagem>();
    /// <summary>Avisos do condutor.</summary>
    public DbSet<Aviso> Avisos => Set<Aviso>();
    /// <summary>Notificações pessoais.</summary>
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();
    /// <summary>Dispositivos Android para push.</summary>
    public DbSet<Dispositivo> Dispositivos => Set<Dispositivo>();
    /// <summary>Tokens de confirmação de conta.</summary>
    public DbSet<ConfirmacaoConta> ConfirmacoesConta => Set<ConfirmacaoConta>();
    /// <summary>Tokens JWT revogados pelo fim de sessão.</summary>
    public DbSet<TokenRevogado> TokensRevogados => Set<TokenRevogado>();

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AtualizarColunasPesquisa();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AtualizarColunasPesquisa();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // P-12 (D-009): a pesquisa não distingue maiúsculas nem acentos e usa a hora de Portugal. Nem uma collation própria
    // nem uma conversão de fuso funcionariam em SQLite nos testes, por isso os valores normalizados ficam em colunas,
    // escritas aqui para nenhuma gravação as esquecer.
    private void AtualizarColunasPesquisa()
    {
        foreach (var entrada in ChangeTracker.Entries<Viagem>())
        {
            if (entrada.State is not (EntityState.Added or EntityState.Modified)) continue;

            entrada.Entity.OrigemPesquisa = TextoPesquisa.Normalizar(entrada.Entity.Origem);
            entrada.Entity.DestinoPesquisa = TextoPesquisa.Normalizar(entrada.Entity.Destino);
            entrada.Entity.PartidaMinutoDoDia = HoraDePortugal.MinutoDoDia(entrada.Entity.PartidaEm);
        }
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<ConversorDataUtc>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<ConversorDataUtc>();
    }

    /// <summary>Configura índices, tamanhos, precisões, conversões, restrições e chaves estrangeiras.</summary>
    /// <remarks>
    /// Qualquer alteração aqui exige uma migration nova (ver <c>database/scripts/README.md</c>); confirmar com
    /// <c>dotnet ef migrations has-pending-model-changes</c>.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Utilizador>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Utilizador>().Property(x => x.Email).HasMaxLength(255);
        modelBuilder.Entity<Utilizador>().Property(x => x.Nome).HasMaxLength(100);
        modelBuilder.Entity<Viagem>().HasOne(x => x.Condutor).WithMany().HasForeignKey(x => x.CondutorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Viagem>().Property(x => x.DistanciaKm).HasPrecision(10, 2);
        modelBuilder.Entity<Viagem>().Property(x => x.OrigemLatitude).HasPrecision(9, 6);
        modelBuilder.Entity<Viagem>().Property(x => x.OrigemLongitude).HasPrecision(9, 6);
        modelBuilder.Entity<Viagem>().Property(x => x.DestinoLatitude).HasPrecision(9, 6);
        modelBuilder.Entity<Viagem>().Property(x => x.DestinoLongitude).HasPrecision(9, 6);
        // Modelo de dados v06: o estado só aceita os nomes de EstadoViagem, sem acento (DC-32).
        modelBuilder.Entity<Viagem>().Property(x => x.Estado).HasConversion<string>();
        modelBuilder.Entity<Viagem>().ToTable(t => t.HasCheckConstraint(
            "CK_Viagens_Estado",
            $"[Estado] IN ('{nameof(EstadoViagem.Ativa)}', '{nameof(EstadoViagem.Indisponibilizada)}', '{nameof(EstadoViagem.Concluida)}')"));

        // Controlo de concorrência otimista na base de dados SQL Server.
        // Em SQLite (testes) não há rowversion: a concorrência só é testada em SQL Server (RegrasSqlServerTests, IT023).
        if (Database.IsSqlServer())
        {
            modelBuilder.Entity<Viagem>()
                .Property(v => v.Versao)
                .IsRowVersion();
            modelBuilder.Entity<Participacao>().Property(p => p.Versao).IsRowVersion();
        }

        modelBuilder.Entity<Participacao>().HasOne(x => x.Viagem).WithMany().HasForeignKey(x => x.ViagemId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Participacao>().HasOne(x => x.Passageiro).WithMany().HasForeignKey(x => x.PassageiroId).OnDelete(DeleteBehavior.Restrict);
        // Os enums são guardados pelo nome (texto), não pelo número.
        modelBuilder.Entity<Participacao>().Property(x => x.Estado).HasConversion<string>();
        modelBuilder.Entity<Participacao>().Property(x => x.RecolhaLatitude).HasPrecision(9, 6);
        modelBuilder.Entity<Participacao>().Property(x => x.RecolhaLongitude).HasPrecision(9, 6);
        modelBuilder.Entity<Participacao>().Property(x => x.DestinoLatitude).HasPrecision(9, 6);
        modelBuilder.Entity<Participacao>().Property(x => x.DestinoLongitude).HasPrecision(9, 6);
        modelBuilder.Entity<Notificacao>().Property(x => x.Tipo).HasConversion<string>();
        modelBuilder.Entity<Aviso>().Property(x => x.Conteudo).HasMaxLength(100);
        // I038 - Impedir pedidos ativos duplicados do mesmo passageiro.
        modelBuilder.Entity<Participacao>()
            .HasIndex(x => new { x.ViagemId, x.PassageiroId })
            .IsUnique()
            .HasFilter("[Estado] IN ('Pendente', 'Aceite')");
        // I030 v2 — integridade referencial (sem eliminacao em cascata)
        modelBuilder.Entity<ConfirmacaoConta>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.UtilizadorId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Dispositivo>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.UtilizadorId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<LigacaoExterna>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.UtilizadorId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Avaliacao>().HasOne<Viagem>().WithMany().HasForeignKey(x => x.ViagemId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Avaliacao>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.AvaliadorId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Avaliacao>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.AvaliadoId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Mensagem>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.RemetenteId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Mensagem>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.DestinatarioId).OnDelete(DeleteBehavior.NoAction);
        // Conversa paginada por cursor (BE-13, BE-33): filtra pelo par e ordena pela data de envio.
        modelBuilder.Entity<Mensagem>()
            .HasIndex(x => new { x.RemetenteId, x.DestinatarioId, x.EnviadoEm })
            .HasDatabaseName("IX_Mensagens_RemetenteId_DestinatarioId_EnviadoEm");
        modelBuilder.Entity<Aviso>().HasOne<Viagem>().WithMany().HasForeignKey(x => x.ViagemId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Notificacao>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.UtilizadorId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Notificacao>().HasOne<Viagem>().WithMany().HasForeignKey(x => x.ViagemId).OnDelete(DeleteBehavior.NoAction);
        // RF-F007-04 (I048): uma só avaliação por par, seja qual for a viagem.
        modelBuilder.Entity<Avaliacao>().HasIndex(x => new { x.AvaliadorId, x.AvaliadoId }).IsUnique();
        modelBuilder.Entity<Dispositivo>().HasIndex(x => x.Token).IsUnique();
        modelBuilder.Entity<TokenRevogado>().HasIndex(x => x.Jti).IsUnique();
        modelBuilder.Entity<ConfirmacaoConta>().HasIndex(x => x.TokenHash).IsUnique();
        ConfigurarIndicesDasConsultas(modelBuilder);
        ConfigurarTamanhos(modelBuilder);
        ConfigurarRestricoes(modelBuilder, Database.IsSqlServer());
    }

    // Modelo de dados v07, §9 (BE-33): índices das consultas mais frequentes. Cada um começa pelo filtro da consulta e
    // acaba na ordenação, para o SQL Server não ter de ordenar em memória.
    private static void ConfigurarIndicesDasConsultas(ModelBuilder modelBuilder)
    {
        // Pesquisa de viagens (RF-F003-01): ativas e futuras, por ordem de partida. As colunas incluídas são as que a
        // pesquisa filtra a seguir (texto normalizado e lugares), para não voltar à tabela por cada viagem.
        modelBuilder.Entity<Viagem>()
            .HasIndex(x => new { x.Estado, x.PartidaEm })
            .IncludeProperties(x => new { x.OrigemPesquisa, x.DestinoPesquisa, x.LugaresDisponiveis });
        // Pedidos pendentes e participações aceites de uma viagem (RF-F004-08, RF-F003-07).
        modelBuilder.Entity<Participacao>().HasIndex(x => new { x.ViagemId, x.Estado });
        // Lista de conversas (RF-F005-09): mensagens recebidas pela data de envio.
        modelBuilder.Entity<Mensagem>().HasIndex(x => new { x.DestinatarioId, x.EnviadoEm });
        // Avisos de uma viagem por ordem de criação (RF-F005-03).
        modelBuilder.Entity<Aviso>().HasIndex(x => new { x.ViagemId, x.CriadoEm });
        // Notificações do utilizador, das mais recentes para as mais antigas (RF-F006-07).
        modelBuilder.Entity<Notificacao>()
            .HasIndex(x => new { x.UtilizadorId, x.CriadaEm })
            .IsDescending(false, true);
    }

    // DC-36 / IN-07: tamanhos do modelo de dados v07. Os serviços validam os mesmos limites, para responder 400 e não 500.
    private static void ConfigurarTamanhos(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Utilizador>().Property(x => x.PasswordHash).HasMaxLength(255);
        modelBuilder.Entity<Utilizador>().Property(x => x.Curso).HasMaxLength(150);
        modelBuilder.Entity<Utilizador>().Property(x => x.Instituicao).HasMaxLength(150);
        modelBuilder.Entity<LigacaoExterna>().Property(x => x.Url).HasMaxLength(2048);
        modelBuilder.Entity<LigacaoExterna>().Property(x => x.Descricao).HasMaxLength(100);
        modelBuilder.Entity<ConfirmacaoConta>().Property(x => x.TokenHash).HasMaxLength(450);
        modelBuilder.Entity<Dispositivo>().Property(x => x.Token).HasMaxLength(450);
        modelBuilder.Entity<Viagem>().Property(x => x.Origem).HasMaxLength(200);
        modelBuilder.Entity<Viagem>().Property(x => x.Destino).HasMaxLength(200);
        // A normalização pode aumentar o número de caracteres de alguns alfabetos.
        modelBuilder.Entity<Viagem>().Property(x => x.OrigemPesquisa).HasMaxLength(400);
        modelBuilder.Entity<Viagem>().Property(x => x.DestinoPesquisa).HasMaxLength(400);
        modelBuilder.Entity<Viagem>().Property(x => x.Estado).HasMaxLength(20);
        modelBuilder.Entity<Participacao>().Property(x => x.Estado).HasMaxLength(20);
        modelBuilder.Entity<Participacao>().Property(x => x.Recolha).HasMaxLength(200);
        modelBuilder.Entity<Participacao>().Property(x => x.Destino).HasMaxLength(200);
        modelBuilder.Entity<Mensagem>().Property(x => x.Conteudo).HasMaxLength(1000);
        modelBuilder.Entity<Notificacao>().Property(x => x.Tipo).HasMaxLength(30);
        modelBuilder.Entity<Notificacao>().Property(x => x.Titulo).HasMaxLength(150);
        modelBuilder.Entity<Notificacao>().Property(x => x.Conteudo).HasMaxLength(1000);
    }

    // DC-36, BE-33: restrições do modelo de dados v07 que se aplicam às colunas existentes. Os estados e tipos são os
    // nomes dos enums, sem acento. Só em SQL Server: ISJSON, que o SQLite não tem, e as comparações numéricas das
    // colunas decimais, que o SQLite guarda como texto (comparava "-8" com "-90" letra a letra). As outras correm
    // também nos testes em SQLite.
    private static void ConfigurarRestricoes(ModelBuilder modelBuilder, bool sqlServer)
    {
        // P-26: o nome não pode ser só espaços.
        modelBuilder.Entity<Utilizador>().ToTable(t => t.HasCheckConstraint("CK_Utilizadores_Nome", "TRIM([Nome]) <> ''"));

        // P-27: só ligações http ou https.
        modelBuilder.Entity<LigacaoExterna>().ToTable(t =>
            t.HasCheckConstraint("CK_LigacoesExternas_Url", "[Url] LIKE 'http://%' OR [Url] LIKE 'https://%'"));

        modelBuilder.Entity<Viagem>().ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_Viagens_Lugares",
                "[LugaresOferecidos] BETWEEN 1 AND 8 AND [LugaresDisponiveis] BETWEEN 0 AND [LugaresOferecidos]");
            t.HasCheckConstraint(
                "CK_Viagens_Locais",
                "NOT ([OrigemLatitude] = [DestinoLatitude] AND [OrigemLongitude] = [DestinoLongitude])");

            if (!sqlServer) return;

            t.HasCheckConstraint("CK_Viagens_Rota", "[RotaPontos] IS NULL OR ISJSON([RotaPontos]) = 1");
            // Latitude entre -90 e 90 e longitude entre -180 e 180.
            t.HasCheckConstraint(
                "CK_Viagens_Coordenadas",
                "[OrigemLatitude] BETWEEN -90 AND 90 AND [DestinoLatitude] BETWEEN -90 AND 90"
                + " AND [OrigemLongitude] BETWEEN -180 AND 180 AND [DestinoLongitude] BETWEEN -180 AND 180");
            // Distância e duração positivas. Ficam a NULL enquanto o Serviço de Mapas não as calculou (P-19).
            t.HasCheckConstraint(
                "CK_Viagens_Medidas",
                "([DistanciaKm] IS NULL OR [DistanciaKm] > 0) AND ([DuracaoMinutos] IS NULL OR [DuracaoMinutos] > 0)");
        });

        modelBuilder.Entity<Participacao>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_Participacoes_Estado", $"[Estado] IN ({Nomes<EstadoParticipacao>()})");
            t.HasCheckConstraint(
                "CK_Participacoes_Locais",
                "NOT ([RecolhaLatitude] = [DestinoLatitude] AND [RecolhaLongitude] = [DestinoLongitude])");
            // P-20: pelo menos um ponto no pedido.
            t.HasCheckConstraint("CK_Participacoes_UmPonto", "[Recolha] IS NOT NULL OR [Destino] IS NOT NULL");
            // A8: um ponto indicado tem coordenadas; um ponto omitido não tem nenhuma.
            t.HasCheckConstraint(
                "CK_Participacoes_CoordRecolha",
                "([Recolha] IS NULL AND [RecolhaLatitude] IS NULL AND [RecolhaLongitude] IS NULL)"
                + " OR ([Recolha] IS NOT NULL AND [RecolhaLatitude] IS NOT NULL AND [RecolhaLongitude] IS NOT NULL)");
            t.HasCheckConstraint(
                "CK_Participacoes_CoordDestino",
                "([Destino] IS NULL AND [DestinoLatitude] IS NULL AND [DestinoLongitude] IS NULL)"
                + " OR ([Destino] IS NOT NULL AND [DestinoLatitude] IS NOT NULL AND [DestinoLongitude] IS NOT NULL)");
            // D-029: uma participação aceite guarda a data da aceitação (histórico do passageiro).
            t.HasCheckConstraint("CK_Participacoes_AceiteEm", "[Estado] <> 'Aceite' OR [AceiteEm] IS NOT NULL");
        });

        // D-019: o aviso não pode ser só espaços.
        modelBuilder.Entity<Aviso>().ToTable(t => t.HasCheckConstraint("CK_Avisos_Conteudo", "TRIM([Conteudo]) <> ''"));

        modelBuilder.Entity<Avaliacao>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_Avaliacoes_Classificacao", "[Classificacao] BETWEEN 1 AND 5");
            t.HasCheckConstraint("CK_Avaliacoes_Distintos", "[AvaliadorId] <> [AvaliadoId]");
        });

        modelBuilder.Entity<Mensagem>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_Mensagens_Distintos", "[RemetenteId] <> [DestinatarioId]");
            t.HasCheckConstraint("CK_Mensagens_Conteudo", "[Conteudo] <> ''");
        });

        modelBuilder.Entity<Notificacao>().ToTable(t =>
            t.HasCheckConstraint("CK_Notificacoes_Tipo", $"[Tipo] IN ({Nomes<TipoNotificacao>()})"));
    }

    private static string Nomes<TEnum>() where TEnum : struct, Enum =>
        string.Join(", ", Enum.GetNames<TEnum>().Select(nome => $"'{nome}'"));
}
