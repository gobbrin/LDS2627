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
    IReadOnlyList<string> OrdemSimulada);

/// <summary>Passageiro aceite de uma viagem. Um ponto a <c>null</c> segue a origem ou o destino da viagem (P-20).</summary>
public sealed record PassageiroAceite(Guid Id, Guid PassageiroId, string Nome, string? Recolha, string? Destino);

/// <summary>Pedido pendente de uma viagem.</summary>
public sealed record PedidoPendente(Guid Id, Guid PassageiroId, string Nome, string? Recolha, string? Destino, DateTime CriadoEm);
