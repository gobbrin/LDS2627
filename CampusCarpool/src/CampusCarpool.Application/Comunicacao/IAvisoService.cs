using CampusCarpool.Application.Comum;

namespace CampusCarpool.Application.Comunicacao;

/// <summary>Avisos do condutor aos passageiros aceites de uma viagem (RF-F005-05 a RF-F005-07).</summary>
public interface IAvisoService
{
    /// <summary>Cria um aviso e uma notificação para cada passageiro aceite (RF-F005-05, RF-F006-05).</summary>
    /// <returns>
    /// O aviso; erro 400 com texto vazio ou com mais de 100 caracteres (P-28, RF-F005-10); 403 se não for o condutor
    /// (RF-F005-07); 404 se a viagem não existir; 409 se a viagem não estiver ativa (DEF-BE-017).
    /// </returns>
    Task<Resultado<AvisoCriado>> CriarAsync(Guid viagemId, Guid condutorId, string? conteudo, CancellationToken ct);

    /// <summary>Avisos de uma viagem, do mais recente para o mais antigo (RF-F005-06).</summary>
    /// <returns>A lista; erro 403 se não for o condutor nem passageiro aceite; 404 se a viagem não existir.</returns>
    Task<Resultado<List<AvisoDaViagem>>> ListarAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct);
}
