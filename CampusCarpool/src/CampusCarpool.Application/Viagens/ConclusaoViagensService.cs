using CampusCarpool.Application.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace CampusCarpool.Application.Viagens;

/// <summary>
/// Implementação de <see cref="IConclusaoViagensService"/> (DEF-BE-009).
/// </summary>
/// <remarks>
/// Cada viagem é concluída e gravada à parte (UC-F002-05, E1), descartando o que ficou por gravar entre viagens.
/// A conclusão automática não cria notificações.
/// </remarks>
public sealed class ConclusaoViagensService(
    IRepositorioViagens viagens,
    IRepositorioParticipacoes participacoes,
    IUnidadeDeTrabalho unidadeDeTrabalho,
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
        var candidatas = await viagens.ListarAtivasJaPartidasAsync(agora, ct);

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
                if (await ConcluirAsync(viagemId, agora, ct))
                    concluidas++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // UC-F002-05, E1: uma viagem que falhe não impede a conclusão das restantes.
                _falhaConclusaoViagem(logger, viagemId, ex);
            }
            finally
            {
                unidadeDeTrabalho.DescartarAlteracoes();
            }
        }

        return concluidas;
    }

    // Conclui uma viagem e cancela os seus pendentes; devolve false se já não estava Ativa.
    private async Task<bool> ConcluirAsync(Guid viagemId, DateTime agora, CancellationToken ct)
    {
        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);

        // Pode ter sido concluída, indisponibilizada ou alterada pelo condutor entretanto.
        if (viagem is null || !viagem.DeveConcluir(agora))
            return false;

        viagem.Concluir(agora);

        // RF-F004-19: só os pedidos pendentes são cancelados; as participações
        // aceites mantêm-se, para o histórico e a avaliação (P-16).
        foreach (var p in await participacoes.ListarPendentesParaAlterarAsync(viagemId, ct))
            p.CancelarPorConclusao();

        // O token Versao (SQL Server) impede sobrepor uma alteração concorrente.
        await unidadeDeTrabalho.ConfirmarAsync(ct);
        return true;
    }
}
