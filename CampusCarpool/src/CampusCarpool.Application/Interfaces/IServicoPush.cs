namespace CampusCarpool.Application.Interfaces;
public interface IServicoPush
{
    Task EnviarAsync(Guid utilizadorId, string titulo, string mensagem, string tipo, CancellationToken ct);
}
