using CampusCarpool.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Persistence;

public sealed class CampusCarpoolDbContext(DbContextOptions<CampusCarpoolDbContext> options) : DbContext(options)
{
    public DbSet<Utilizador> Utilizadores => Set<Utilizador>();
    public DbSet<Viagem> Viagens => Set<Viagem>();
    public DbSet<Participacao> Participacoes => Set<Participacao>();
    public DbSet<LigacaoExterna> LigacoesExternas => Set<LigacaoExterna>();
    public DbSet<Avaliacao> Avaliacoes => Set<Avaliacao>();
    public DbSet<Mensagem> Mensagens => Set<Mensagem>();
    public DbSet<Aviso> Avisos => Set<Aviso>();
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();
    public DbSet<Dispositivo> Dispositivos => Set<Dispositivo>();
    public DbSet<ConfirmacaoConta> ConfirmacoesConta => Set<ConfirmacaoConta>();
    public DbSet<TokenRevogado> TokensRevogados => Set<TokenRevogado>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Utilizador>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Utilizador>().Property(x => x.Email).HasMaxLength(255);
        b.Entity<Utilizador>().Property(x => x.Nome).HasMaxLength(100);
        b.Entity<Viagem>().HasOne(x => x.Condutor).WithMany().HasForeignKey(x => x.CondutorId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Viagem>().Property(x => x.DistanciaKm).HasPrecision(10, 2);
        b.Entity<Viagem>().Property(x => x.OrigemLatitude).HasPrecision(9, 6);
        b.Entity<Viagem>().Property(x => x.OrigemLongitude).HasPrecision(9, 6);
        b.Entity<Viagem>().Property(x => x.DestinoLatitude).HasPrecision(9, 6);
        b.Entity<Viagem>().Property(x => x.DestinoLongitude).HasPrecision(9, 6);

        // Controlo de concorrência otimista na base de dados SQL Server.
        if (Database.IsSqlServer())
        {
            b.Entity<Viagem>()
                .Property(v => v.Versao)
                .IsRowVersion();
        }

        b.Entity<Participacao>().HasOne(x => x.Viagem).WithMany().HasForeignKey(x => x.ViagemId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Participacao>().HasOne(x => x.Passageiro).WithMany().HasForeignKey(x => x.PassageiroId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Participacao>().Property(x => x.Estado).HasConversion<string>();
        b.Entity<Notificacao>().Property(x => x.Tipo).HasConversion<string>();
        b.Entity<Aviso>().Property(x => x.Conteudo).HasMaxLength(100);
        // I038 - Impedir pedidos ativos duplicados do mesmo passageiro.
        b.Entity<Participacao>()
            .HasIndex(x => new { x.ViagemId, x.PassageiroId })
            .IsUnique()
            .HasFilter("[Estado] IN ('Pendente', 'Aceite')");
        // I030 v2 — integridade referencial (sem eliminacao em cascata)
        b.Entity<ConfirmacaoConta>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.UtilizadorId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Dispositivo>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.UtilizadorId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<LigacaoExterna>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.UtilizadorId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Avaliacao>().HasOne<Viagem>().WithMany().HasForeignKey(x => x.ViagemId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Avaliacao>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.AvaliadorId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Avaliacao>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.AvaliadoId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Mensagem>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.RemetenteId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Mensagem>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.DestinatarioId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Mensagem>().HasOne<Viagem>().WithMany().HasForeignKey(x => x.ViagemId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Aviso>().HasOne<Viagem>().WithMany().HasForeignKey(x => x.ViagemId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Aviso>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.CondutorId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Notificacao>().HasOne<Utilizador>().WithMany().HasForeignKey(x => x.UtilizadorId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Notificacao>().HasOne<Viagem>().WithMany().HasForeignKey(x => x.ViagemId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Avaliacao>().HasIndex(x => new { x.ViagemId, x.AvaliadorId, x.AvaliadoId }).IsUnique();
        b.Entity<Dispositivo>().HasIndex(x => x.Token).IsUnique();
        b.Entity<TokenRevogado>().HasIndex(x => x.Jti).IsUnique();
        b.Entity<ConfirmacaoConta>().HasIndex(x => x.TokenHash).IsUnique();
    }
}
