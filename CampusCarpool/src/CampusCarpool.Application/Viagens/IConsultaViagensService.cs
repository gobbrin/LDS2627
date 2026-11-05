using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Viagens;

/// <summary>Dados públicos de um condutor nos resultados e nos detalhes de uma viagem (RF-F003-04).</summary>
public sealed record CondutorViagem(Guid Id, string Nome, decimal? ClassificacaoMedia);

/// <summary>Regras comuns às consultas de viagens: quem pode consultar e os dados do condutor.</summary>
public interface IConsultaViagensService
{
    /// <summary>
    /// AD-003 §5.1 (A7, G20): outro utilizador só consulta uma viagem pesquisável. O condutor e quem tem qualquer
    /// pedido na viagem consultam-na sempre.
    /// </summary>
    Task<bool> PodeConsultarAsync(Viagem viagem, Guid? utilizadorId, CancellationToken ct);

    /// <summary>Nome e classificação média dos condutores indicados, por identificador.</summary>
    /// <remarks>A média tem uma casa decimal e é <c>null</c> sem avaliações, como no perfil público (P-10).</remarks>
    Task<Dictionary<Guid, CondutorViagem>> CondutoresAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
}
