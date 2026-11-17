using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Notificacoes;
using CampusCarpool.Application.Rotas;

namespace CampusCarpool.Application.Viagens;

/// <summary>Implementação de <see cref="ICicloDeVidaDaViagemService"/>: indisponibilizar e concluir uma viagem.</summary>
/// <remarks>
/// Saiu do <c>ViagemService</c> na fase R (AE-02). As transições de estado estão na entidade <c>Viagem</c> (AD-001);
/// aqui coordenam-se os dados e as notificações. Nenhuma das duas operações chama o Serviço de Mapas (P-19).
/// </remarks>
public sealed class CicloDeVidaDaViagemService(
    IRepositorioViagens viagens,
    IRepositorioParticipacoes participacoes,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    INotificacaoService notificacoes) : ICicloDeVidaDaViagemService
{
    /// <inheritdoc />
    public async Task<Resultado> IndisponibilizarAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct)
    {
        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);
        if (viagem is null) return ErrosDaViagem.NaoEncontrada;
        if (viagem.CondutorId != utilizadorId) return ErrosDaViagem.Proibido;
        if (!viagem.EstaAtiva) return new Erro(TipoErro.Conflito, "A viagem já não está ativa.");

        viagem.Indisponibilizar();

        // P-25 / RF-F002-10 (DEF-BE-008): cancela os pedidos pendentes e as participações aceites.
        var afetadas = await participacoes.ListarAtivasParaAlterarAsync(viagemId, ct);

        foreach (var participacao in afetadas)
            participacao.CancelarPorIndisponibilidade();

        // BE-29: a indisponibilização não chama os mapas (P-19). A viagem já não se realiza, por isso não se gasta um
        // pedido para recalcular a rota sem passageiros: sem paragens, a rota guardada continua certa; com paragens,
        // fica por recalcular e só se calcula se alguém a consultar.
        ResultadoRotaComPontos? guardada = RotaGuardada.Ler(viagem);

        if (guardada is not null && guardada.PontosOrdenados.Count > 2)
            viagem.MarcarRotaPorRecalcular();

        // P-23 / RF-F006-03 (DEF-BE-020): notifica os passageiros com pedido pendente ou
        // participação aceite antes da indisponibilização; o condutor originou a alteração.
        notificacoes.RegistarAlteracaoViagem(
            viagem.Id,
            viagem.CondutorId,
            afetadas.Select(p => p.PassageiroId),
            utilizadorId,
            "A viagem foi marcada como indisponível pelo condutor e o teu pedido ou participação foi cancelado.");

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return Resultado.Sucesso;
    }

    /// <inheritdoc />
    public async Task<Resultado> ConcluirAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct)
    {
        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);
        if (viagem is null) return ErrosDaViagem.NaoEncontrada;
        if (viagem.CondutorId != utilizadorId) return ErrosDaViagem.Proibido;

        var agora = DateTime.UtcNow;

        if (!viagem.PodeConcluir(agora))
            return new Erro(TipoErro.Conflito, "A viagem só pode ser concluída depois da partida e enquanto estiver ativa.");

        // RF-F004-19: os pedidos pendentes são cancelados com a conclusão (regra na entidade, BE-31).
        var pendentes = await participacoes.ListarPendentesParaAlterarAsync(viagemId, ct);
        viagem.Concluir(agora, pendentes);

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return Resultado.Sucesso;
    }
}
