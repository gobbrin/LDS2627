using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Application.Services;

/// <summary>
/// Gere a aceitação, rejeição e cancelamento de participações.
/// Utiliza abstrações de persistência, sem depender do EF Core.
/// </summary>
/// <remarks>
/// Padrão comum aos três métodos: (1) carregar a participação e a viagem (rastreadas); (2) verificar a
/// permissão e o estado; (3) alterar as entidades e gravar com <see cref="IUnidadeDeTrabalho.Confirmar"/>;
/// (4) só depois enviar o evento em tempo real e criar as notificações.
/// Os lugares são verificados contra a contagem real de aceites antes de mexer em <c>LugaresDisponiveis</c>;
/// se não baterem, a operação devolve <see cref="ResultadoGestaoParticipacao.DadosInconsistentes"/>.
/// A coluna <c>Versao</c> da viagem protege a aceitação e o cancelamento de uma aceite (que mudam a viagem);
/// a rejeição e o cancelamento de um pendente só mudam a participação, que não tem versão (DO-03).
/// </remarks>
public sealed class ParticipacaoService(
    IRepositorioParticipacoes repositorioParticipacoes,
    IRepositorioViagens repositorioViagens,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    INotificacaoService notificacaoService,
    INotificadorPedidosTempoReal notificadorPedidosTempoReal) : IParticipacaoService
{
    /// <inheritdoc />
    public async Task<ResultadoGestaoParticipacao> AceitarAsync(
        Guid participacaoId,
        Guid utilizadorId,
        CancellationToken ct)
    {
        var participacao = await repositorioParticipacoes
            .ObterPorIdAsync(participacaoId, ct);

        if (participacao is null)
            return ResultadoGestaoParticipacao.NaoEncontrada;

        var viagem = await repositorioViagens
            .ObterPorIdAsync(participacao.ViagemId, ct);

        if (viagem is null)
            return ResultadoGestaoParticipacao.NaoEncontrada;

        if (viagem.CondutorId != utilizadorId)
            return ResultadoGestaoParticipacao.NaoAutorizado;

        // RF-F004-10 / AD-001, regra 3 (F5): só pedidos pendentes de viagens ativas, mesmo depois da hora de partida.
        if (participacao.Estado != EstadoParticipacao.Pendente
            || viagem.Estado != nameof(EstadoViagem.Ativa))
        {
            return ResultadoGestaoParticipacao.Conflito;
        }

        var aceites = await repositorioParticipacoes.ListarPorViagemAsync(
            viagem.Id,
            EstadoParticipacao.Aceite,
            ct);

        // RF-F002-13: lugares disponíveis = oferecidos - aceites. O valor guardado tem de bater com a contagem.
        var lugaresCalculados = viagem.LugaresOferecidos - aceites.Count;

        if (lugaresCalculados != viagem.LugaresDisponiveis
            || lugaresCalculados < 0)
        {
            return ResultadoGestaoParticipacao.DadosInconsistentes;
        }

        if (lugaresCalculados == 0)
            return ResultadoGestaoParticipacao.SemCapacidade;

        participacao.Estado = EstadoParticipacao.Aceite;
        // AceiteEm fica mesmo que a participação seja cancelada depois (histórico, RF-F009-08).
        participacao.AceiteEm = DateTime.UtcNow;
        viagem.LugaresDisponiveis--;

        // Guarda a decisão e a alteração de capacidade.
        // Conflitos no token Versao são tratados pela unidade de trabalho.
        unidadeDeTrabalho.Confirmar();

        // A decisão já foi confirmada; envia o evento ao passageiro.
        await notificadorPedidosTempoReal.PedidoAceiteAsync(
              participacao.PassageiroId,
              participacao.Id,
              ct);

        // As notificações são criadas depois da confirmação da operação.
        await notificacaoService.CriarAsync(
            participacao.PassageiroId,
            TipoNotificacao.PedidoAceite,
            "O teu pedido de participação na viagem foi aceite.",
            ct);

        // RF-F006-14: os outros passageiros aceites são avisados do novo passageiro (sem push).
        var restantes = await repositorioParticipacoes.ListarPorViagemAsync(
            viagem.Id,
            EstadoParticipacao.Aceite,
            ct);

        foreach (var passageiroId in restantes
            .Where(p => p.PassageiroId != participacao.PassageiroId)
            .Select(p => p.PassageiroId)
            .Distinct())
        {
            await notificacaoService.CriarAsync(
                passageiroId,
                TipoNotificacao.AlteracaoViagem,
                "Um novo passageiro foi aceite na viagem em que participas.",
                ct);
        }

        return ResultadoGestaoParticipacao.Sucesso;
    }

    /// <inheritdoc />
    public async Task<ResultadoGestaoParticipacao> RejeitarAsync(
        Guid participacaoId,
        Guid utilizadorId,
        CancellationToken ct)
    {
        var participacao = await repositorioParticipacoes
            .ObterPorIdAsync(participacaoId, ct);

        if (participacao is null)
            return ResultadoGestaoParticipacao.NaoEncontrada;

        var viagem = await repositorioViagens
            .ObterPorIdAsync(participacao.ViagemId, ct);

        if (viagem is null)
            return ResultadoGestaoParticipacao.NaoEncontrada;

        if (viagem.CondutorId != utilizadorId)
            return ResultadoGestaoParticipacao.NaoAutorizado;

        if (participacao.Estado != EstadoParticipacao.Pendente)
            return ResultadoGestaoParticipacao.Conflito;

        participacao.Estado = EstadoParticipacao.Rejeitada;

        // Guarda a decisão. A rejeição não mexe na viagem, por isso o token Versao não protege
        // contra uma aceitação ou um cancelamento simultâneos do mesmo pedido (DO-03).
        unidadeDeTrabalho.Confirmar();

        // A decisão já foi confirmada; envia o evento ao passageiro.
        await notificadorPedidosTempoReal.PedidoRejeitadoAsync(
              participacao.PassageiroId,
              participacao.Id,
              ct);

        await notificacaoService.CriarAsync(
            participacao.PassageiroId,
            TipoNotificacao.PedidoRejeitado,
            "O teu pedido de participação na viagem foi rejeitado.",
            ct);

        return ResultadoGestaoParticipacao.Sucesso;
    }

    /// <inheritdoc />
    public async Task<ResultadoGestaoParticipacao> CancelarAsync(
        Guid participacaoId,
        Guid utilizadorId,
        CancellationToken ct)
    {
        var participacao = await repositorioParticipacoes
            .ObterPorIdAsync(participacaoId, ct);

        if (participacao is null)
            return ResultadoGestaoParticipacao.NaoEncontrada;

        var viagem = await repositorioViagens
            .ObterPorIdAsync(participacao.ViagemId, ct);

        if (viagem is null)
            return ResultadoGestaoParticipacao.NaoEncontrada;

        // AD-001 (F1, DC-35): só o passageiro cancela; o condutor rejeita pendentes ou indisponibiliza a viagem.
        if (participacao.PassageiroId != utilizadorId)
            return ResultadoGestaoParticipacao.NaoAutorizado;

        if (participacao.Estado != EstadoParticipacao.Pendente
            && participacao.Estado != EstadoParticipacao.Aceite)
        {
            return ResultadoGestaoParticipacao.Conflito;
        }

        var eraAceite = participacao.Estado == EstadoParticipacao.Aceite;

        if (eraAceite)
        {
            // P-03: uma participação aceite só pode ser cancelada até à hora de partida.
            if (viagem.PartidaEm <= DateTime.UtcNow)
                return ResultadoGestaoParticipacao.Conflito;

            var aceites = await repositorioParticipacoes.ListarPorViagemAsync(
                viagem.Id,
                EstadoParticipacao.Aceite,
                ct);

            var lugaresCalculados = viagem.LugaresOferecidos - aceites.Count;

            if (lugaresCalculados != viagem.LugaresDisponiveis
                || aceites.Count == 0)
            {
                return ResultadoGestaoParticipacao.DadosInconsistentes;
            }

            // RF-F004-17: liberta o lugar (recalculado a partir dos aceites, sem contar este).
            viagem.LugaresDisponiveis =
                viagem.LugaresOferecidos - (aceites.Count - 1);
        }

        participacao.Estado = EstadoParticipacao.Cancelada;

        unidadeDeTrabalho.Confirmar();

        // P-21: cancelar um pedido pendente não gera notificação.
        // DEF-BE-019 (fica para m4): RF-F006-02 pede que o condutor seja notificado quando um passageiro
        // cancela; aqui só são notificados os outros passageiros aceites.
        if (eraAceite)
        {
            var restantes = await repositorioParticipacoes.ListarPorViagemAsync(
                viagem.Id,
                EstadoParticipacao.Aceite,
                ct);

            foreach (var passageiroId in restantes
                .Select(p => p.PassageiroId)
                .Distinct())
            {
                await notificacaoService.CriarAsync(
                    passageiroId,
                    TipoNotificacao.AlteracaoViagem,
                    "Um passageiro cancelou a sua participação na viagem.",
                    ct);
            }
        }

        return ResultadoGestaoParticipacao.Sucesso;
    }
}
