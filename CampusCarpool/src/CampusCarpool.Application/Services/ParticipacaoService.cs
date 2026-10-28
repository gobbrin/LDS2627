using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Application.Services;

// Gere a aceitação, rejeição e cancelamento de participações.
// Utiliza abstrações de persistência, sem depender do EF Core.
public sealed class ParticipacaoService(
    IRepositorioParticipacoes repositorioParticipacoes,
    IRepositorioViagens repositorioViagens,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    INotificacaoService notificacaoService) : IParticipacaoService
{
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

        if (participacao.Estado != EstadoParticipacao.Pendente
            || viagem.Estado != "Ativa"
            || viagem.PartidaEm <= DateTime.UtcNow)
        {
            return ResultadoGestaoParticipacao.Conflito;
        }

        var aceites = await repositorioParticipacoes.ListarPorViagemAsync(
            viagem.Id,
            EstadoParticipacao.Aceite,
            ct);

        var lugaresCalculados = viagem.LugaresOferecidos - aceites.Count;

        if (lugaresCalculados != viagem.LugaresDisponiveis
            || lugaresCalculados < 0)
        {
            return ResultadoGestaoParticipacao.DadosInconsistentes;
        }

        if (lugaresCalculados == 0)
            return ResultadoGestaoParticipacao.SemCapacidade;

        participacao.Estado = EstadoParticipacao.Aceite;
        viagem.LugaresDisponiveis--;

        // Guarda a decisão e a alteração de capacidade.
        // Conflitos no token Versao são tratados pela unidade de trabalho.
        unidadeDeTrabalho.Confirmar();

        // As notificações são criadas depois da confirmação da operação.
        await notificacaoService.CriarAsync(
            participacao.PassageiroId,
            TipoNotificacao.PedidoAceite,
            "O teu pedido de participação na viagem foi aceite.",
            ct);

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

        unidadeDeTrabalho.Confirmar();

        await notificacaoService.CriarAsync(
            participacao.PassageiroId,
            TipoNotificacao.PedidoRejeitado,
            "O teu pedido de participação na viagem foi rejeitado.",
            ct);

        return ResultadoGestaoParticipacao.Sucesso;
    }

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

        if (participacao.PassageiroId != utilizadorId
            && viagem.CondutorId != utilizadorId)
        {
            return ResultadoGestaoParticipacao.NaoAutorizado;
        }

        if (participacao.Estado != EstadoParticipacao.Pendente
            && participacao.Estado != EstadoParticipacao.Aceite)
        {
            return ResultadoGestaoParticipacao.Conflito;
        }

        var eraAceite = participacao.Estado == EstadoParticipacao.Aceite;

        if (eraAceite)
        {
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

            viagem.LugaresDisponiveis =
                viagem.LugaresOferecidos - (aceites.Count - 1);
        }

        participacao.Estado = EstadoParticipacao.Cancelada;

        unidadeDeTrabalho.Confirmar();

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
