using CampusCarpool.Application.Interfaces;

namespace CampusCarpool.Tests;

public sealed class ServicoPushFalso : IServicoPush
{
    public Task EnviarAsync(
        Guid utilizadorId,
        string titulo,
        string mensagem,
        string tipo,
        CancellationToken ct)
        => Task.CompletedTask;
}

public sealed class NotificadorPedidosFalso : INotificadorPedidosTempoReal
{
    public Task PedidoRecebidoAsync(
        Guid destinatarioId, Guid pedidoId, CancellationToken ct)
        => Task.CompletedTask;

    public Task PedidoAceiteAsync(
        Guid destinatarioId, Guid pedidoId, CancellationToken ct)
        => Task.CompletedTask;

    public Task PedidoRejeitadoAsync(
        Guid destinatarioId, Guid pedidoId, CancellationToken ct)
        => Task.CompletedTask;
}

public sealed class ServicoPushRegistador : IServicoPush
{
    public List<(Guid UtilizadorId, string Titulo,
        string Mensagem, string Tipo)> Envios { get; } = new();

    public bool SimularFalha { get; set; }

    public Task EnviarAsync(
        Guid utilizadorId,
        string titulo,
        string mensagem,
        string tipo,
        CancellationToken ct)
    {
        if (SimularFalha)
            throw new HttpRequestException("Falha simulada no serviço push.");

        Envios.Add((utilizadorId, titulo, mensagem, tipo));

        return Task.CompletedTask;
    }
}