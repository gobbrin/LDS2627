using CampusCarpool.Application.Abstractions.Integrations;
using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Rotas;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Rules;

namespace CampusCarpool.Application.Viagens;

/// <summary>
/// Regras e cálculo da rota de uma alteração de origem ou destino (RF-F002-07, RF-F002-16, RF-F008-05).
/// </summary>
/// <remarks>
/// <para>Usada em dois sítios, para as duas operações aplicarem exatamente as mesmas regras (AE-01):</para>
/// <list type="bullet">
/// <item><c>ViagemService.EditarAsync</c> (<c>PUT /viagens/{id}</c>): valida, calcula a rota e grava a viagem.</item>
/// <item><c>SimulacaoDeAlteracaoService.SimularAlteracaoAsync</c> (<c>POST /viagens/{id}/simular-alteracao</c>):
/// valida e calcula a rota, sem gravar nada.</item>
/// </list>
/// <para>A validação dos locais (<see cref="ValidarLocais"/>) também serve a criação da viagem. Não tem estado: os
/// dois serviços criam-na com o Serviço de Mapas e o serviço de rotas que já recebem.</para>
/// </remarks>
internal sealed class AlteracaoDeViagem(IServicoMapasRotas mapas, IRotaService rotas)
{
    // Tamanho máximo dos textos de origem e destino (coluna da BD).
    private const int TamanhoMaximoLocal = 200;

    /// <summary>Locais e rota calculados para uma alteração que muda a origem ou o destino.</summary>
    /// <param name="Locais">Texto e coordenadas da nova origem e do novo destino.</param>
    /// <param name="Rota">Rota com as paragens dos passageiros aceites.</param>
    public sealed record NovaRota(LocaisViagem Locais, ResultadoRotaComPontos Rota);

    /// <summary>
    /// Regras de quem pode alterar (ou simular a alteração de) uma viagem: só o condutor, com a viagem ativa e ainda
    /// por partir (RF-F002-07).
    /// </summary>
    /// <param name="viagem">Viagem a alterar.</param>
    /// <param name="utilizadorId">Utilizador que pede a alteração.</param>
    /// <param name="agora">Instante da verificação, em UTC.</param>
    /// <returns>O erro 403 ou 409; <c>null</c> se a alteração for permitida.</returns>
    public static Erro? ValidarQuemAltera(Viagem viagem, Guid utilizadorId, DateTime agora)
    {
        if (viagem.CondutorId != utilizadorId)
            return ErrosDaViagem.Proibido;

        if (!viagem.PorPartir(agora))
            return new Erro(TipoErro.Conflito, "Viagem não editável.");

        return null;
    }

    /// <summary>
    /// Valida a origem e o destino pelo texto: preenchidos, até 200 caracteres e diferentes (criação, alteração e
    /// simulação da alteração, RF-F002-04, RF-F002-16).
    /// </summary>
    /// <param name="origem">Texto da origem.</param>
    /// <param name="destino">Texto do destino.</param>
    /// <returns>O erro da primeira regra que falha; <c>null</c> se estiver tudo certo.</returns>
    public static Erro? ValidarLocais(string origem, string destino)
    {
        if (string.IsNullOrWhiteSpace(origem))
            return new Erro(TipoErro.DadosInvalidos, "Indica a origem da viagem.");

        if (string.IsNullOrWhiteSpace(destino))
            return new Erro(TipoErro.DadosInvalidos, "Indica o destino da viagem.");

        if (origem.Length > TamanhoMaximoLocal)
            return new Erro(TipoErro.DadosInvalidos, "A origem tem no máximo 200 caracteres.");

        if (destino.Length > TamanhoMaximoLocal)
            return new Erro(TipoErro.DadosInvalidos, "O destino tem no máximo 200 caracteres.");

        // RF-F002-04: origem e destino diferentes (aqui pelo texto; pelas coordenadas depois de geocodificar).
        if (RegraMesmoLocal.MesmoTexto(origem, destino))
            return ErrosDaViagem.MesmoLocal;

        return null;
    }

    /// <summary>Indica se duas coordenadas são o mesmo local (RF-F002-04, depois de geocodificar).</summary>
    /// <param name="a">Primeiro ponto.</param>
    /// <param name="b">Segundo ponto.</param>
    /// <returns><c>true</c> se forem o mesmo local.</returns>
    public static bool MesmasCoordenadas(Coordenadas a, Coordenadas b) =>
        RegraMesmoLocal.MesmasCoordenadas(a.Latitude, a.Longitude, b.Latitude, b.Longitude);

    /// <summary>
    /// Obtém as coordenadas dos locais novos e calcula a rota com os passageiros aceites (RF-F002-07, RF-F008-05).
    /// Não altera a viagem.
    /// </summary>
    /// <param name="viagem">Viagem a alterar (dá as coordenadas do local que não muda).</param>
    /// <param name="novaOrigem">Texto da nova origem, já validado.</param>
    /// <param name="novoDestino">Texto do novo destino, já validado.</param>
    /// <param name="aceites">Participações aceites (as suas paragens entram na rota).</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>Os locais e a rota; ou o erro: 400 mesmo local, 422 local ou rota não encontrados, 502/503/504 mapas.</returns>
    public async Task<Resultado<NovaRota>> CalcularRotaComNovosLocaisAsync(
        Viagem viagem,
        string novaOrigem,
        string novoDestino,
        IReadOnlyList<Participacao> aceites,
        CancellationToken ct)
    {
        if (!mapas.Configurado)
            return ChamadaMapas.NaoConfigurado;

        return await ChamadaMapas.ExecutarAsync<NovaRota>(
            async limite =>
            {
                // Só o local que muda é geocodificado de novo; o mesmo texto noutra capitalização não muda (BE-38 c).
                var pontoOrigem = RegraMesmoLocal.MesmoTexto(novaOrigem, viagem.Origem)
                    ? new Coordenadas(viagem.OrigemLatitude, viagem.OrigemLongitude)
                    : await mapas.GeocodificarAsync(novaOrigem, limite);
                var pontoDestino = RegraMesmoLocal.MesmoTexto(novoDestino, viagem.Destino)
                    ? new Coordenadas(viagem.DestinoLatitude, viagem.DestinoLongitude)
                    : await mapas.GeocodificarAsync(novoDestino, limite);

                if (pontoOrigem is null || pontoDestino is null)
                    return ErrosDaViagem.LocalNaoEncontrado;

                if (MesmasCoordenadas(pontoOrigem, pontoDestino))
                    return ErrosDaViagem.MesmoLocal;

                var rota = await rotas.CalcularEntreAsync(
                    new PontoRota(null, TiposPontoRota.Origem, novaOrigem, pontoOrigem),
                    new PontoRota(null, TiposPontoRota.DestinoViagem, novoDestino, pontoDestino),
                    aceites,
                    limite);

                if (rota is null)
                    return ErrosDaViagem.RotaNaoCalculada;

                return new NovaRota(new LocaisViagem(novaOrigem, pontoOrigem, novoDestino, pontoDestino), rota);
            },
            ErrosDaViagem.MapasIndisponiveis,
            ErrosDaViagem.TempoExcedido,
            ct);
    }
}
