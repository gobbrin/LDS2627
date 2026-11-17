using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Notificacoes;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Rules;

namespace CampusCarpool.Application.Participacoes;

/// <summary>
/// Gere a submissão, a aceitação, a rejeição e o cancelamento de participações (a simulação está no
/// <see cref="SimulacaoDePedidoService"/>, AE-01).
/// Utiliza abstrações de persistência, sem depender do EF Core.
/// </summary>
/// <remarks>
/// Padrão comum a aceitar, rejeitar e cancelar: (1) carregar a participação e a viagem (rastreadas); (2) verificar a
/// permissão e o estado; (3) alterar as entidades, registar as notificações e gravar tudo de uma vez com
/// <see cref="IUnidadeDeTrabalho.ConfirmarAsync"/> (DC-37); (4) só depois enviar o evento em tempo real e o push, que
/// já não dependem do cliente: a operação aconteceu, mesmo que o pedido HTTP seja cancelado entretanto.
/// As transições estão nas entidades (AD-001). Os lugares são verificados contra a contagem real de aceites;
/// se não baterem, a operação é recusada, para não agravar o erro.
/// As colunas <c>Versao</c> da viagem e da participação recusam a segunda de duas decisões simultâneas (DO-03).
/// </remarks>
public sealed class ParticipacaoService(
    IRepositorioParticipacoes repositorioParticipacoes,
    IRepositorioViagens repositorioViagens,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    INotificacaoService notificacaoService,
    INotificadorPedidosTempoReal notificadorPedidosTempoReal,
    IRotaService rotaService,
    IServicoMapasRotas mapas) : IParticipacaoService
{
    private static readonly Erro ViagemIndisponivel = new(TipoErro.Conflito, "Viagem indisponível.");
    private static readonly Erro NaoEncontrada = new(TipoErro.NaoEncontrado, "Pedido não encontrado.");
    private static readonly Erro Proibido = new(TipoErro.Proibido, "Não tens permissão para gerir este pedido.");
    private static readonly Erro EstadoNaoPermite = new(TipoErro.Conflito, "O estado atual do pedido não permite esta operação.");
    private static readonly Erro SemCapacidade = new(TipoErro.Conflito, "Sem lugares disponíveis.");
    private static readonly Erro LugaresInconsistentes = new(TipoErro.Conflito, "A capacidade da viagem está inconsistente.");
    private static readonly Erro RotaIndisponivel = new(
        TipoErro.ServicoIndisponivel,
        "Serviço de mapas indisponível: não foi possível recalcular a rota.");

    // As regras do pedido antes de gravar; as mesmas da simulação do passageiro (POST /viagens/{id}/simular-pedido).
    private readonly PreparacaoDePedido preparacao = new(repositorioParticipacoes, mapas);

    /// <inheritdoc />
    /// <remarks>
    /// Passos, por ordem (BE-05): (1) validar o texto dos pontos, sem BD; (2) ler a viagem; (3) validar a viagem, os
    /// pontos e obter as coordenadas (<see cref="PreparacaoDePedido.PrepararAsync"/>); (4) confirmar que a viagem
    /// continua ativa; (5) gravar e avisar o condutor.
    /// </remarks>
    public async Task<Resultado<PedidoCriado>> CriarPedidoAsync(
        Guid viagemId,
        Guid passageiroId,
        string? recolha,
        string? destino,
        CancellationToken ct)
    {
        // 1. P-20: os pontos validam-se primeiro, porque não precisam da BD.
        if (PreparacaoDePedido.ValidarTextoDosPontos(recolha, destino) is { } pontosInvalidos)
            return pontosInvalidos;

        // 2. A viagem tem de existir.
        Viagem? viagem = await repositorioViagens.ObterPorIdAsync(viagemId, ct);

        if (viagem is null)
            return new Erro(TipoErro.NaoEncontrado, "Viagem não encontrada.");

        // 3. Regras da viagem e dos pontos, e coordenadas (RF-F004-01, 05, 06, 18, 20, 21; RF-F008-01).
        Resultado<Participacao> preparado = await preparacao.PrepararAsync(viagem, passageiroId, recolha, destino, ct);

        if (preparado.Erro is { } recusado)
            return recusado;

        // 4. Os mapas podem demorar até 10 s e a gravação do pedido não toca na linha da viagem, por isso Versao não
        // deteta a mudança. Sem isto, o pedido ficaria Pendente para sempre numa viagem já terminada.
        if (await repositorioViagens.ObterEstadoAsync(viagem.Id, ct) != EstadoViagem.Ativa)
            return ViagemIndisponivel;

        // 5. O pedido fica pendente, sem descontar lugares.
        return await GravarENotificarPedidoAsync(viagem, preparado.Valor, ct);
    }

    /// <summary>
    /// Grava o pedido com a notificação do condutor e, só depois, envia o push e o evento em tempo real (RF-F006-01).
    /// </summary>
    /// <returns>O pedido criado; 409 se outro pedido simultâneo do mesmo passageiro ganhou (índice único da BD).</returns>
    private async Task<Resultado<PedidoCriado>> GravarENotificarPedidoAsync(
        Viagem viagem,
        Participacao pedido,
        CancellationToken ct)
    {
        Notificacao notificacao = notificacaoService.Registar(
            viagem.CondutorId,
            TipoNotificacao.PedidoRecebido,
            "Recebeste um novo pedido de participação numa viagem.",
            viagem.Id);

        // Grava o pedido e a notificação. Dois pedidos simultâneos podem passar a verificação acima; a BD recusa o segundo.
        if (!await repositorioParticipacoes.AdicionarPedidoAsync(pedido, ct))
            return new Erro(TipoErro.Conflito, PreparacaoDePedido.MensagemPedidoRepetido);

        await notificacaoService.EnviarPushAsync(notificacao, CancellationToken.None);
        await notificadorPedidosTempoReal.PedidoRecebidoAsync(viagem.CondutorId, pedido.Id, CancellationToken.None);

        return new PedidoCriado(
            pedido.Id,
            pedido.ViagemId,
            pedido.PassageiroId,
            pedido.Estado.ToString(),
            pedido.Recolha,
            pedido.Destino,
            pedido.CriadoEm);
    }

    /// <inheritdoc />
    public async Task<Resultado> AceitarAsync(
        Guid participacaoId,
        Guid utilizadorId,
        CancellationToken ct)
    {
        var participacao = await repositorioParticipacoes
            .ObterPorIdAsync(participacaoId, ct);

        if (participacao is null)
            return NaoEncontrada;

        var viagem = await repositorioViagens
            .ObterPorIdAsync(participacao.ViagemId, ct);

        if (viagem is null)
            return NaoEncontrada;

        if (viagem.CondutorId != utilizadorId)
            return Proibido;

        // RF-F004-10 / AD-001, regra 3 (F5): só pedidos pendentes de viagens ativas, mesmo depois da hora de partida.
        if (!participacao.EstaPendente || !viagem.EstaAtiva)
            return EstadoNaoPermite;

        var aceites = await repositorioParticipacoes.ListarPorViagemAsync(
            viagem.Id,
            EstadoParticipacao.Aceite,
            ct);

        // RF-F002-13: lugares disponíveis = oferecidos - aceites. O valor guardado tem de bater com a contagem.
        if (!viagem.LugaresConsistentes(aceites.Count))
            return LugaresInconsistentes;

        if (!viagem.TemLugarLivre)
            return SemCapacidade;

        // RF-F008-05 / P-19: um pedido com paragens novas recalcula a rota; sem o Serviço de Mapas é recusado e nada
        // muda. Sem paragens novas a rota guardada continua certa e não se chamam os mapas (decisão de 16/11, BE-03).
        if (RotaService.AcrescentaParagens(viagem, participacao))
        {
            bool rotaAtualizada = await rotaService.RecalcularAsync(viagem, [.. aceites, participacao], ct);

            if (!rotaAtualizada)
                return RotaIndisponivel;
        }

        participacao.Aceitar(DateTime.UtcNow);
        viagem.OcuparLugar();

        var notificacao = notificacaoService.Registar(
            participacao.PassageiroId,
            TipoNotificacao.PedidoAceite,
            "O teu pedido de participação na viagem foi aceite.",
            viagem.Id);

        // RF-F006-14 / P-23: o novo passageiro recebe Pedido aceite e não Alteração da viagem pela sua entrada.
        notificacaoService.RegistarAlteracaoViagem(
            viagem.Id,
            viagem.CondutorId,
            aceites.Select(p => p.PassageiroId),
            utilizadorId,
            "Um novo passageiro foi aceite na viagem em que participas.");

        // Um conflito nas colunas Versao chega ao cliente como 409, pela unidade de trabalho.
        await unidadeDeTrabalho.ConfirmarAsync(ct);

        await notificadorPedidosTempoReal.PedidoAceiteAsync(participacao.PassageiroId, participacao.Id, CancellationToken.None);
        await notificacaoService.EnviarPushAsync(notificacao, CancellationToken.None);

        return Resultado.Sucesso;
    }

    /// <inheritdoc />
    public async Task<Resultado> RejeitarAsync(
        Guid participacaoId,
        Guid utilizadorId,
        CancellationToken ct)
    {
        var participacao = await repositorioParticipacoes
            .ObterPorIdAsync(participacaoId, ct);

        if (participacao is null)
            return NaoEncontrada;

        var viagem = await repositorioViagens
            .ObterPorIdAsync(participacao.ViagemId, ct);

        if (viagem is null)
            return NaoEncontrada;

        if (viagem.CondutorId != utilizadorId)
            return Proibido;

        if (!participacao.EstaPendente)
            return EstadoNaoPermite;

        participacao.Rejeitar();

        var notificacao = notificacaoService.Registar(
            participacao.PassageiroId,
            TipoNotificacao.PedidoRejeitado,
            "O teu pedido de participação na viagem foi rejeitado.",
            viagem.Id);

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        await notificadorPedidosTempoReal.PedidoRejeitadoAsync(participacao.PassageiroId, participacao.Id, CancellationToken.None);
        await notificacaoService.EnviarPushAsync(notificacao, CancellationToken.None);

        return Resultado.Sucesso;
    }

    /// <inheritdoc />
    public async Task<Resultado> CancelarAsync(
        Guid participacaoId,
        Guid utilizadorId,
        CancellationToken ct)
    {
        var participacao = await repositorioParticipacoes
            .ObterPorIdAsync(participacaoId, ct);

        if (participacao is null)
            return NaoEncontrada;

        var viagem = await repositorioViagens
            .ObterPorIdAsync(participacao.ViagemId, ct);

        if (viagem is null)
            return NaoEncontrada;

        // AD-001 (F1, DC-35): só o passageiro cancela; o condutor rejeita pendentes ou indisponibiliza a viagem.
        if (participacao.PassageiroId != utilizadorId)
            return Proibido;

        var agora = DateTime.UtcNow;

        // Pendente, ou aceite até à hora de partida (P-03).
        if (!participacao.PodeSerCanceladaPeloPassageiro(viagem, agora))
            return EstadoNaoPermite;

        if (participacao.EstaAceite)
        {
            var aceites = await repositorioParticipacoes.ListarPorViagemAsync(
                viagem.Id,
                EstadoParticipacao.Aceite,
                ct);

            if (aceites.Count == 0 || !viagem.LugaresConsistentes(aceites.Count))
                return LugaresInconsistentes;

            // RF-F004-17: liberta o lugar.
            viagem.LibertarLugar();

            // RF-F008-05 / P-19: o cancelamento nunca é recusado; sem o Serviço de Mapas, a rota fica por recalcular.
            // Uma participação sem paragens não está na rota, por isso a rota guardada mantém-se (BE-03).
            if (RotaService.AcrescentaParagens(viagem, participacao))
            {
                bool rotaAtualizada = await rotaService.RecalcularAsync(
                    viagem,
                    aceites.Where(p => p.Id != participacao.Id).ToList(),
                    ct);

                if (!rotaAtualizada)
                    viagem.MarcarRotaPorRecalcular();
            }

            // RF-F006-02 e RF-F006-14 / P-23: são notificados o condutor e os restantes passageiros aceites.
            // P-21: cancelar um pedido pendente não gera notificação.
            notificacaoService.RegistarAlteracaoViagem(
                viagem.Id,
                viagem.CondutorId,
                aceites.Where(p => p.Id != participacao.Id).Select(p => p.PassageiroId),
                utilizadorId,
                "Um passageiro cancelou a sua participação na viagem.");
        }

        participacao.CancelarPeloPassageiro(viagem, agora);

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return Resultado.Sucesso;
    }
}
