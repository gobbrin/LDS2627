using CampusCarpool.Application.Rotas;

namespace CampusCarpool.Application.Participacoes;

/// <summary>Pedido de participação acabado de criar.</summary>
/// <param name="Id">Identificador da participação.</param>
/// <param name="ViagemId">Viagem.</param>
/// <param name="PassageiroId">Passageiro.</param>
/// <param name="Estado">Nome do estado (<c>Pendente</c>).</param>
/// <param name="Recolha">Ponto de recolha; <c>null</c> segue a origem da viagem (P-20).</param>
/// <param name="Destino">Ponto de destino; <c>null</c> segue o destino da viagem (P-20).</param>
/// <param name="CriadoEm">Data do pedido.</param>
public sealed record PedidoCriado(
    Guid Id,
    Guid ViagemId,
    Guid PassageiroId,
    string Estado,
    string? Recolha,
    string? Destino,
    DateTime CriadoEm);

/// <summary>Resultado da simulação: rota original e simulada (distância em km, duração em minutos, ordem dos pontos) e o desvio.</summary>
/// <remarks>
/// <c>RotaSimulada</c> (BE-35, D-2, I112) é a rota se o pedido for aceite, no formato de <c>GET /rotas/viagens/{id}</c>:
/// pontos com coordenadas, troços, custo com o novo passageiro e a linha do mapa (<c>geometriaAproximada</c> a
/// <c>false</c> quando vem da HERE). Os campos anteriores mantêm-se.
/// </remarks>
public sealed record SimulacaoPedido(
    Guid ViagemId,
    Guid PedidoId,
    string OrigemViagem,
    string DestinoViagem,
    string RecolhaPassageiro,
    string DestinoPassageiro,
    decimal DistanciaOriginalKm,
    int DuracaoOriginalMinutos,
    decimal DistanciaSimuladaKm,
    int DuracaoSimuladaMinutos,
    decimal DesvioDistanciaKm,
    int DesvioDuracaoMinutos,
    IReadOnlyList<string> OrdemOriginal,
    IReadOnlyList<string> OrdemSimulada,
    RotaDaViagem RotaSimulada);

/// <summary>
/// Simulação de um pedido ainda por enviar, vista pelo passageiro (<c>POST /viagens/{id}/simular-pedido</c>; ecrã 14 do
/// protótipo, RF-F003-08, RF-F008-09).
/// </summary>
/// <remarks>
/// Não tem a ordem dos locais nem o id do pedido (o pedido não existe). <c>RotaSimulada</c> tem o formato de
/// <c>GET /rotas/viagens/{id}</c>, mas só com a origem, o destino e os pontos do próprio passageiro (P-13): as paragens
/// dos aceites não aparecem nos pontos nem nos troços. A linha (<c>geometria</c>) segue o percurso real, que passa
/// por essas paragens.
/// </remarks>
/// <param name="ViagemId">Viagem.</param>
/// <param name="RecolhaPassageiro">Recolha efetiva (a indicada, ou a origem da viagem se omitida, P-20).</param>
/// <param name="DestinoPassageiro">Destino efetivo (o indicado, ou o destino da viagem se omitido, P-20).</param>
/// <param name="DistanciaOriginalKm">Distância da rota atual.</param>
/// <param name="DuracaoOriginalMinutos">Duração da rota atual.</param>
/// <param name="DistanciaSimuladaKm">Distância se o pedido for aceite.</param>
/// <param name="DuracaoSimuladaMinutos">Duração se o pedido for aceite.</param>
/// <param name="DesvioDistanciaKm">Diferença de distância.</param>
/// <param name="DesvioDuracaoMinutos">Diferença de duração.</param>
/// <param name="RotaSimulada">Rota simulada para o mapa, só com os pontos que o passageiro pode ver.</param>
public sealed record SimulacaoNovoPedido(
    Guid ViagemId,
    string RecolhaPassageiro,
    string DestinoPassageiro,
    decimal DistanciaOriginalKm,
    int DuracaoOriginalMinutos,
    decimal DistanciaSimuladaKm,
    int DuracaoSimuladaMinutos,
    decimal DesvioDistanciaKm,
    int DesvioDuracaoMinutos,
    RotaDaViagem RotaSimulada);

/// <summary>Passageiro aceite de uma viagem. Um ponto a <c>null</c> segue a origem ou o destino da viagem (P-20).</summary>
/// <param name="Id">
/// Id da participação: só o condutor o recebe; para um passageiro é <c>null</c>, para não ver as participações dos
/// outros (BE-37, como o BE-06 fez na rota).
/// </param>
/// <param name="PassageiroId">Passageiro (para abrir o perfil ou enviar mensagem).</param>
/// <param name="Nome">Nome do passageiro.</param>
/// <param name="Recolha">Ponto de recolha, ou <c>null</c> para a origem da viagem.</param>
/// <param name="Destino">Ponto de destino, ou <c>null</c> para o destino da viagem.</param>
public sealed record PassageiroAceite(Guid? Id, Guid PassageiroId, string Nome, string? Recolha, string? Destino);

/// <summary>Pedido pendente de uma viagem.</summary>
public sealed record PedidoPendente(Guid Id, Guid PassageiroId, string Nome, string? Recolha, string? Destino, DateTime CriadoEm);
