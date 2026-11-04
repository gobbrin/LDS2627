using CampusCarpool.Application.Interfaces;

namespace CampusCarpool.Tests;

/// <summary>Serviço de push que não faz nada (para testes que não verificam o push).</summary>
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

/// <summary>Notificador em tempo real falso: regista os eventos para o teste os verificar.</summary>
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

/// <summary>Serviço de push falso que guarda cada envio, para verificar destinatários e conteúdo.</summary>
public sealed class ServicoPushRegistador : IServicoPush
{
    public List<(Guid UtilizadorId, string Titulo,
        string Mensagem, string Tipo)> Envios
    { get; } = new();

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
