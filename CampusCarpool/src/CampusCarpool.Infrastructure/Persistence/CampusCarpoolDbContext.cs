using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Persistence;

/// <summary>
/// Contexto EF Core da aplicação: uma tabela por entidade do Domain, em SQL Server (testes: SQLite em memória).
/// </summary>
/// <remarks>
/// Toda a configuração do modelo está em <see cref="OnModelCreating"/>; os ficheiros em <c>Configurations/</c>
/// estão vazios (DC-24). O esquema real das BD de produção vem das migrations (<c>Migrations/</c>), aplicadas à
/// parte; os testes usam <c>EnsureCreated</c>, que segue este modelo. Os dois divergem no índice das avaliações
/// (ver abaixo, IN-01). Todas as datas são UTC: <see cref="ConfigureConventions"/> aplica o <see cref="ConversorDataUtc"/>.
/// Não há restrições <c>CHECK</c> além do estado da viagem, nem tamanhos na maior parte dos textos (DO-07).
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
        modelBuilder.Entity<Mensagem>().HasOne<Viagem>().WithMany().HasForeignKey(x => x.ViagemId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Aviso>().HasOne<Viagem>().WithMany().HasForeignKey(x => x.ViagemId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Aviso>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.CondutorId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Notificacao>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.UtilizadorId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Notificacao>().HasOne<Viagem>().WithMany().HasForeignKey(x => x.ViagemId).OnDelete(DeleteBehavior.NoAction);
        // IN-01 / DEF-BE-025 (fica para m4): a migration I048_AvaliacoesPorPar trocou este índice pelo par
        // (AvaliadorId, AvaliadoId), mas o modelo e o snapshot continuam com o triplo. Ao corrigir aqui, escrever
        // o Up da migration à mão (um "migrations add" tentaria apagar um índice que as BD migradas já não têm).
        modelBuilder.Entity<Avaliacao>().HasIndex(x => new { x.ViagemId, x.AvaliadorId, x.AvaliadoId }).IsUnique();
        modelBuilder.Entity<Dispositivo>().HasIndex(x => x.Token).IsUnique();
        modelBuilder.Entity<TokenRevogado>().HasIndex(x => x.Jti).IsUnique();
        modelBuilder.Entity<ConfirmacaoConta>().HasIndex(x => x.TokenHash).IsUnique();
    }
}
