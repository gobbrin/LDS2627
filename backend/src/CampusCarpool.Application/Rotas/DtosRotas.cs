namespace CampusCarpool.Application.Rotas;

// Respostas da API de rotas (BE-06): tipos próprios, sem tipos do Domain nem da integração. O modelo interno
// (ResultadoRotaComPontos, SegmentoRota, PontoRota) está em ModeloRota.cs; a conversão faz-se em RespostasDaRota.

/// <summary>Par de coordenadas devolvido ao cliente (<c>GET /rotas/geocodificar</c> e <see cref="RotaCalculada"/>).</summary>
public sealed record CoordenadasResposta(decimal Latitude, decimal Longitude);

/// <summary>Ponto da rota, tal como o cliente o vê (RF-F008-03).</summary>
/// <remarks>
/// Não leva o id da participação: cada passageiro veria as participações dos outros (BE-06). O cliente identifica a
/// sua recolha pelo <c>Local</c>, que é o texto que indicou no pedido.
/// </remarks>
/// <param name="Ordem">Posição na rota, a começar em 0 (a origem).</param>
/// <param name="Tipo"><c>Origem</c>, <c>Recolha</c>, <c>DestinoPassageiro</c> ou <c>DestinoViagem</c>.</param>
/// <param name="Local">Texto do local.</param>
/// <param name="Latitude">Latitude.</param>
/// <param name="Longitude">Longitude.</param>
public sealed record PontoDaRota(int Ordem, string Tipo, string Local, decimal Latitude, decimal Longitude);

/// <summary>Troço entre dois pontos consecutivos da rota, identificados pela ordem (RF-F008-04).</summary>
/// <param name="DeOrdem">Ordem do ponto de partida do troço.</param>
/// <param name="AteOrdem">Ordem do ponto de chegada do troço.</param>
/// <param name="DistanciaKm">Distância do troço, em km.</param>
/// <param name="DuracaoMinutos">Duração do troço, em minutos.</param>
public sealed record TrocoDaRota(int DeOrdem, int AteOrdem, decimal DistanciaKm, int DuracaoMinutos);

/// <summary>Rota de uma viagem, tal como o utilizador a pode ver, com a estimativa de custo.</summary>
/// <remarks>
/// <c>Geometria</c> é a linha a desenhar no mapa, como lista de pares <c>[latitude, longitude]</c> (RF-F008-07,
/// DEF-BE-054). <c>GeometriaAproximada</c> é <c>true</c> quando a linha são só retas entre os pontos (a HERE não
/// está configurada ou falhou).
/// </remarks>
public sealed record RotaDaViagem(
    Guid ViagemId,
    int ParticipacoesAceites,
    IReadOnlyList<PontoDaRota> PontosOrdenados,
    IReadOnlyList<TrocoDaRota> Segmentos,
    decimal DistanciaTotalKm,
    int DuracaoTotalMinutos,
    ResultadoEstimativaCusto EstimativaCusto,
    IReadOnlyList<decimal[]> Geometria,
    bool GeometriaAproximada);

/// <summary>Rota direta entre dois locais, sem gravar nada (<c>POST /rotas/calcular</c>, RF-F008-02).</summary>
/// <remarks>Mesmos campos que antes da I112, mais a linha do mapa, no formato de <see cref="RotaDaViagem"/>.</remarks>
public sealed record RotaCalculada(
    decimal DistanciaKm,
    int DuracaoMinutos,
    CoordenadasResposta Origem,
    CoordenadasResposta Destino,
    IReadOnlyList<decimal[]> Geometria,
    bool GeometriaAproximada);

/// <summary>Estimativa de custo devolvida ao cliente (RF-F008-10, RF-F008-11).</summary>
/// <remarks><c>Indicativo</c> é sempre <c>true</c>: o valor é só uma referência para dividir despesas, não um preço.</remarks>
public sealed record ResultadoEstimativaCusto(
    decimal DistanciaKm,
    decimal PrecoPorKm,
    int PassageirosAceites,
    int TotalParticipantes,
    decimal CustoTotalEstimado,
    decimal CustoPorParticipante,
    bool Indicativo,
    string Moeda
);
