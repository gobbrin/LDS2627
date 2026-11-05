using CampusCarpool.Application.Viagens;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusCarpool.Infrastructure.Services;

/// <summary>
/// Implementação de <see cref="IConclusaoViagensService"/> sobre o EF Core (DEF-BE-009).
/// </summary>
/// <remarks>
/// Cada viagem é concluída e gravada à parte (UC-F002-05, E1), limpando o <c>ChangeTracker</c> entre viagens.
/// A conclusão automática não cria notificações.
/// </remarks>
public sealed class ConclusaoViagensService(
    CampusCarpoolDbContext db,
    ILogger<ConclusaoViagensService> logger) : IConclusaoViagensService
{
    private static readonly Action<ILogger, Guid, Exception?> _falhaConclusaoViagem =
        LoggerMessage.Define<Guid>(
            LogLevel.Error,
            new EventId(1, "FalhaConclusaoViagem"),
            "Falha ao concluir automaticamente a viagem {ViagemId}; nova tentativa na próxima verificação.");

    /// <inheritdoc />
    public async Task<int> ConcluirViagensTerminadasAsync(DateTime agora, CancellationToken ct)
    {
        // A duração não é negativa, por isso a partida já passou em todas as candidatas.
        // A soma da duração é feita em memória para não depender da tradução de datas
        // de cada fornecedor (SQL Server em produção, SQLite nos testes).
        var candidatas = await db.Viagens
            .AsNoTracking()
            .Where(v => v.Estado == nameof(EstadoViagem.Ativa) && v.PartidaEm <= agora)
            .Select(v => new { v.Id, v.PartidaEm, v.DuracaoMinutos })
            .ToListAsync(ct);

        // Sem duração estimada (serviço de mapas indisponível na criação), a viagem
        // conclui-se à hora de partida.
        var terminadas = candidatas
            .Where(v => v.PartidaEm.AddMinutes(v.DuracaoMinutos ?? 0) <= agora)
            .Select(v => v.Id)
            .ToList();

        var concluidas = 0;

        foreach (var viagemId in terminadas)
        {
            try
            {
                if (await ConcluirAsync(viagemId, ct))
                    concluidas++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // UC-F002-05, E1: uma viagem que falhe não impede a conclusão das restantes.
                _falhaConclusaoViagem(logger, viagemId, ex);
            }
            finally
            {
                db.ChangeTracker.Clear();
            }
        }

        return concluidas;
    }

    // Conclui uma viagem e cancela os seus pendentes; devolve false se já não estava Ativa.
    private async Task<bool> ConcluirAsync(Guid viagemId, CancellationToken ct)
    {
        var viagem = await db.Viagens.SingleAsync(v => v.Id == viagemId, ct);

        // Pode ter sido concluída ou indisponibilizada pelo condutor entretanto.
        if (viagem.Estado != nameof(EstadoViagem.Ativa))
            return false;

        viagem.Estado = nameof(EstadoViagem.Concluida);

        // RF-F004-19: só os pedidos pendentes são cancelados; as participações
        // aceites mantêm-se, para o histórico e a avaliação (P-16).
        var pendentes = await db.Participacoes
            .Where(p => p.ViagemId == viagemId && p.Estado == EstadoParticipacao.Pendente)
            .ToListAsync(ct);
        foreach (var p in pendentes)
            p.Estado = EstadoParticipacao.Cancelada;

        // O token Versao (SQL Server) impede sobrepor uma alteração concorrente.
        await db.SaveChangesAsync(ct);
        return true;
    }
}
