using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Rules;

namespace CampusCarpool.Application.Participacoes;

/// <summary>
/// Regras de um pedido de participação antes de ser gravado (RF-F004-01, 02, 05, 06, 18, 20, 21; P-20; RF-F008-01).
/// </summary>
/// <remarks>
/// <para>Usada em dois sítios, para as duas operações aplicarem exatamente as mesmas regras:</para>
/// <list type="bullet">
/// <item><c>ParticipacaoService.CriarPedidoAsync</c> (<c>POST /pedidos</c>): valida, prepara e grava o pedido.</item>
/// <item><c>SimulacaoDePedidoService.SimularNovoPedidoAsync</c> (<c>POST /viagens/{id}/simular-pedido</c>): valida e
/// prepara o pedido, simula a rota e não grava nada (ecrã 14 do protótipo).</item>
/// </list>
/// <para>Não tem estado: os dois serviços criam-na com os repositórios e o Serviço de Mapas que já recebem.</para>
/// </remarks>
public sealed class PreparacaoDePedido(IRepositorioParticipacoes repositorioParticipacoes, IServicoMapasRotas mapas)
{
    /// <summary>Mensagem do 409 de um pedido repetido (RF-F004-06); também usada ao gravar, se a BD recusar o segundo.</summary>
    public const string MensagemPedidoRepetido = "Já existe um pedido ou participação ativa nesta viagem.";

    // Uma mensagem por regra dos pontos do pedido (BE-15).
    private static readonly Erro RecolhaIgualAoDestino = new(TipoErro.DadosInvalidos, "A recolha e o destino não podem ser o mesmo local.");
    private static readonly Erro RecolhaNoDestinoDaViagem = new(TipoErro.DadosInvalidos, "A recolha não pode ser o destino da viagem.");
    private static readonly Erro DestinoNaOrigemDaViagem = new(TipoErro.DadosInvalidos, "O destino não pode ser a origem da viagem.");

    // Coordenadas dos pontos indicados no pedido; um ponto omitido fica a null.
    private sealed record PontosDoPedido(Coordenadas? Recolha, Coordenadas? Destino);

    /// <summary>Valida o texto dos pontos indicados no pedido (P-20), sem consultar a BD nem os mapas.</summary>
    /// <param name="recolha">Ponto de recolha, ou <c>null</c> para a origem da viagem.</param>
    /// <param name="destino">Ponto de destino, ou <c>null</c> para o destino da viagem.</param>
    /// <returns>O erro da primeira regra que falha, com uma mensagem própria; <c>null</c> se estiver tudo certo.</returns>
    public static Erro? ValidarTextoDosPontos(string? recolha, string? destino)
    {
        // P-20: pelo menos um dos dois pontos.
        if (recolha is null && destino is null)
            return new Erro(TipoErro.DadosInvalidos, "Indica um ponto de recolha ou de destino.");

        if (recolha is not null && string.IsNullOrWhiteSpace(recolha))
            return new Erro(TipoErro.DadosInvalidos, "O ponto de recolha está vazio.");

        if (recolha is not null && recolha.Length > Participacao.TamanhoMaximoPonto)
            return new Erro(TipoErro.DadosInvalidos, "O ponto de recolha tem no máximo 200 caracteres.");

        if (destino is not null && string.IsNullOrWhiteSpace(destino))
            return new Erro(TipoErro.DadosInvalidos, "O ponto de destino está vazio.");

        if (destino is not null && destino.Length > Participacao.TamanhoMaximoPonto)
            return new Erro(TipoErro.DadosInvalidos, "O ponto de destino tem no máximo 200 caracteres.");

        return null;
    }

    /// <summary>
    /// Verifica a viagem e os pontos e devolve o pedido pronto a gravar, ainda fora da base de dados.
    /// </summary>
    /// <remarks>
    /// Passos, por ordem (BE-05): (1) a viagem aceita um pedido deste passageiro; (2) mesmo local pelo texto, sem
    /// mapas; (3) coordenadas dos pontos novos; (4) mesmo local pelas coordenadas; (5) criar o pedido em memória.
    /// O texto dos pontos já foi validado com <see cref="ValidarTextoDosPontos"/>.
    /// </remarks>
    /// <param name="viagem">Viagem pedida, já lida da BD.</param>
    /// <param name="passageiroId">Utilizador que pede (ou simula) a participação.</param>
    /// <param name="recolha">Ponto de recolha, ou <c>null</c> para a origem da viagem.</param>
    /// <param name="destino">Ponto de destino, ou <c>null</c> para o destino da viagem.</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>
    /// O pedido Pendente, por gravar; ou o erro: 400 mesmo local, 409 viagem indisponível, própria viagem, pedido
    /// repetido ou sem lugares, 422 ponto não encontrado, 502, 503 ou 504.
    /// </returns>
    public async Task<Resultado<Participacao>> PrepararAsync(
        Viagem viagem,
        Guid passageiroId,
        string? recolha,
        string? destino,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(viagem);

        // 1. A viagem tem de aceitar um pedido deste passageiro.
        if (await ValidarViagemParaPedidoAsync(viagem, passageiroId, ct) is { } viagemRecusada)
            return viagemRecusada;

        // 2. Primeiro só pelo texto, para não chamar o Serviço de Mapas sem necessidade.
        if (ValidarMesmoLocalPorTexto(recolha ?? viagem.Origem, destino ?? viagem.Destino, viagem) is { } mesmoTexto)
            return mesmoTexto;

        // 3. Coordenadas dos pontos indicados.
        Resultado<PontosDoPedido> pontos = await ObterCoordenadasAsync(viagem, recolha, destino, ct);

        if (pontos.Erro is { } erroMapas)
            return erroMapas;

        // 4. As mesmas regras de mesmo local, agora pelas coordenadas.
        if (ValidarMesmoLocalPorCoordenadas(viagem, pontos.Valor) is { } mesmasCoordenadas)
            return mesmasCoordenadas;

        // 5. O pedido fica pendente, sem descontar lugares, e ainda não está na BD.
        return Participacao.Pedir(
            viagem.Id,
            passageiroId,
            recolha,
            pontos.Valor.Recolha,
            destino,
            pontos.Valor.Destino,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Verifica se a viagem aceita um pedido deste passageiro: ativa e por partir, não é do próprio passageiro
    /// (RF-F004-05), sem outro pedido ativo dele (RF-F004-06) e com lugar livre.
    /// </summary>
    /// <returns>O erro da primeira regra que falha; <c>null</c> se a viagem aceitar o pedido.</returns>
    private async Task<Erro?> ValidarViagemParaPedidoAsync(Viagem viagem, Guid passageiroId, CancellationToken ct)
    {
        if (!viagem.PorPartir(DateTime.UtcNow))
            return new Erro(TipoErro.Conflito, "Viagem indisponível.");

        if (viagem.CondutorId == passageiroId)
            return new Erro(TipoErro.Conflito, "Não podes pedir participação na tua própria viagem.");

        if (await repositorioParticipacoes.ExisteAtivaAsync(viagem.Id, passageiroId, ct))
            return new Erro(TipoErro.Conflito, MensagemPedidoRepetido);

        // Só as participações aceites ocupam lugares; a regra é a da viagem (BE-16).
        if (!viagem.TemLugarLivre)
            return new Erro(TipoErro.Conflito, "Não existem lugares disponíveis nesta viagem.");

        return null;
    }

    /// <summary>
    /// Regras de mesmo local pelo texto: RF-F004-20 (recolha no destino da viagem), RF-F004-21 (destino na origem da
    /// viagem) e RF-F004-18 (recolha e destino efetivos). As duas primeiras vêm antes por terem a mensagem mais precisa:
    /// com um ponto omitido, a regra 18 também falharia.
    /// </summary>
    /// <param name="recolha">Recolha efetiva: a indicada ou, se omitida, a origem da viagem.</param>
    /// <param name="destino">Destino efetivo: o indicado ou, se omitido, o destino da viagem.</param>
    /// <param name="viagem">Viagem pedida.</param>
    /// <returns>O erro da regra que falha; <c>null</c> se nenhuma falhar.</returns>
    private static Erro? ValidarMesmoLocalPorTexto(string recolha, string destino, Viagem viagem)
    {
        if (RegraMesmoLocal.MesmoTexto(recolha, viagem.Destino))
            return RecolhaNoDestinoDaViagem;

        if (RegraMesmoLocal.MesmoTexto(destino, viagem.Origem))
            return DestinoNaOrigemDaViagem;

        if (RegraMesmoLocal.MesmoTexto(recolha, destino))
            return RecolhaIgualAoDestino;

        return null;
    }

    /// <summary>
    /// As mesmas regras de <see cref="ValidarMesmoLocalPorTexto"/>, agora pelas coordenadas. Um ponto omitido usa as
    /// coordenadas da viagem (P-20).
    /// </summary>
    /// <param name="viagem">Viagem pedida.</param>
    /// <param name="pontos">Coordenadas dos pontos indicados no pedido.</param>
    /// <returns>O erro da regra que falha; <c>null</c> se nenhuma falhar.</returns>
    private static Erro? ValidarMesmoLocalPorCoordenadas(Viagem viagem, PontosDoPedido pontos)
    {
        Coordenadas origemViagem = new(viagem.OrigemLatitude, viagem.OrigemLongitude);
        Coordenadas destinoViagem = new(viagem.DestinoLatitude, viagem.DestinoLongitude);
        Coordenadas recolha = pontos.Recolha ?? origemViagem;
        Coordenadas destino = pontos.Destino ?? destinoViagem;

        if (MesmasCoordenadas(recolha, destinoViagem))
            return RecolhaNoDestinoDaViagem;

        if (MesmasCoordenadas(destino, origemViagem))
            return DestinoNaOrigemDaViagem;

        if (MesmasCoordenadas(recolha, destino))
            return RecolhaIgualAoDestino;

        return null;
    }

    private static bool MesmasCoordenadas(Coordenadas a, Coordenadas b) =>
        RegraMesmoLocal.MesmasCoordenadas(a.Latitude, a.Longitude, b.Latitude, b.Longitude);

    /// <summary>
    /// Obtém as coordenadas dos pontos indicados no pedido (RF-F008-01). Um ponto omitido fica a <c>null</c> (P-20).
    /// </summary>
    /// <remarks>
    /// Um ponto com o mesmo texto que a origem ou o destino da viagem usa as coordenadas já guardadas. Só um ponto novo
    /// chama o Serviço de Mapas: sem pontos novos, o pedido não depende dele (P-19, BE-03). A geocodificação passa pela
    /// memória de 10 minutos do <c>ServicoMapasComReserva</c> (D-3): simular e pedir logo a seguir geocodifica uma vez.
    /// </remarks>
    /// <returns>As coordenadas, ou o erro dos mapas (422 ponto não encontrado, 502, 503, 504).</returns>
    private async Task<Resultado<PontosDoPedido>> ObterCoordenadasAsync(
        Viagem viagem,
        string? recolha,
        string? destino,
        CancellationToken ct)
    {
        Coordenadas? coordenadasRecolha = CoordenadasJaConhecidas(viagem, recolha);
        Coordenadas? coordenadasDestino = CoordenadasJaConhecidas(viagem, destino);
        bool geocodificarRecolha = recolha is not null && coordenadasRecolha is null;
        bool geocodificarDestino = destino is not null && coordenadasDestino is null;

        if (!geocodificarRecolha && !geocodificarDestino)
            return new PontosDoPedido(coordenadasRecolha, coordenadasDestino);

        if (!mapas.Configurado)
            return ChamadaMapas.NaoConfigurado;

        return await ChamadaMapas.ExecutarAsync<PontosDoPedido>(
            async limite =>
            {
                if (geocodificarRecolha)
                {
                    coordenadasRecolha = await mapas.GeocodificarAsync(recolha!.Trim(), limite);

                    if (coordenadasRecolha is null)
                        return new Erro(TipoErro.NaoProcessavel, "Ponto de recolha não encontrado.");
                }

                if (geocodificarDestino)
                {
                    coordenadasDestino = await mapas.GeocodificarAsync(destino!.Trim(), limite);

                    if (coordenadasDestino is null)
                        return new Erro(TipoErro.NaoProcessavel, "Ponto de destino não encontrado.");
                }

                return new PontosDoPedido(coordenadasRecolha, coordenadasDestino);
            },
            "Serviço de mapas indisponível.",
            "Tempo de resposta do serviço de mapas excedido.",
            ct);
    }

    /// <summary>Coordenadas guardadas da viagem para um texto igual à sua origem ou ao seu destino.</summary>
    /// <returns>As coordenadas; <c>null</c> para um texto omitido ou que é um local novo.</returns>
    private static Coordenadas? CoordenadasJaConhecidas(Viagem viagem, string? texto)
    {
        if (texto is null)
            return null;

        if (RegraMesmoLocal.MesmoTexto(texto, viagem.Origem))
            return new Coordenadas(viagem.OrigemLatitude, viagem.OrigemLongitude);

        if (RegraMesmoLocal.MesmoTexto(texto, viagem.Destino))
            return new Coordenadas(viagem.DestinoLatitude, viagem.DestinoLongitude);

        return null;
    }
}
