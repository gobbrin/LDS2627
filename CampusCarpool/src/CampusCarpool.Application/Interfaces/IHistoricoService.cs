
namespace CampusCarpool.Application.Interfaces;

public interface IHistoricoService
{
    Task<HistoricoResultado> ConsultarAsync(
        Guid utilizadorId,
        CancellationToken ct);
}

public sealed record ViagemHistorico(
    Guid Id,
    string Origem,
    string Destino,
    DateTime PartidaEm,
    string Estado,
    int? LugaresOferecidos,
    Guid? ParticipacaoId,
    string? EstadoParticipacao
);

public sealed record PedidoHistorico(
    Guid Id,
    Guid ViagemId,
    string Origem,
    string Destino,
    DateTime PartidaEm,
    DateTime CriadoEm,
    string Estado
);

public sealed record NotificacaoHistorico(
    Guid Id,
    Guid? ViagemId,
    string Tipo,
    string Titulo,
    string Conteudo,
    DateTime CriadaEm,
    DateTime? LidaEm
);

public sealed record AvisoHistorico(
    Guid Id,
    Guid ViagemId,
    string Conteudo,
    DateTime CriadoEm
);

public sealed record HistoricoResultado(
    IReadOnlyList<ViagemHistorico> ViagensComoCondutor,
    IReadOnlyList<ViagemHistorico> ViagensComoPassageiro,
    IReadOnlyList<PedidoHistorico> PedidosEfetuados,
    IReadOnlyList<PedidoHistorico> PedidosRecebidos,
    IReadOnlyList<NotificacaoHistorico> Notificacoes,
    IReadOnlyList<AvisoHistorico> Avisos
);
