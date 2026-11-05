using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Api.Services;

/// <summary>Dados públicos de um condutor nos resultados e nos detalhes de uma viagem (RF-F003-04).</summary>
public sealed record CondutorViagem(Guid Id, string Nome, decimal? ClassificacaoMedia);

/// <summary>Regras comuns às consultas de viagens: quem pode consultar e os dados do condutor.</summary>
public static class ConsultaViagens
{
    /// <summary>Glossário: Ativa, com data e hora futuras e pelo menos um lugar disponível (RF-F003-02).</summary>
    public static bool Pesquisavel(Viagem viagem) =>
        viagem.Estado == nameof(EstadoViagem.Ativa)
        && viagem.PartidaEm > DateTime.UtcNow
        && viagem.LugaresDisponiveis > 0;

    /// <summary>
    /// AD-003 §5.1 (A7, G20): outro utilizador só consulta uma viagem pesquisável. O condutor e quem tem qualquer
    /// pedido na viagem consultam-na sempre, para o passageiro não a perder quando ela enche.
    /// </summary>
    public static async Task<bool> PodeConsultarAsync(
        CampusCarpoolDbContext db,
        Viagem viagem,
        Guid? utilizadorId,
        CancellationToken ct) =>
        viagem.CondutorId == utilizadorId
        || Pesquisavel(viagem)
        || await db.Participacoes.AnyAsync(p => p.ViagemId == viagem.Id && p.PassageiroId == utilizadorId, ct);

    /// <summary>Nome e classificação média dos condutores indicados, por identificador.</summary>
    /// <remarks>A média tem uma casa decimal e é <c>null</c> sem avaliações, como no perfil público (P-10).</remarks>
    public static async Task<Dictionary<Guid, CondutorViagem>> CondutoresAsync(
        CampusCarpoolDbContext db,
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct)
    {
        var nomes = await db.Utilizadores
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Nome })
            .ToListAsync(ct);

        // Agrupado em memória: o tipo decimal não tem AVG em SQLite (testes).
        var classificacoes = await db.Avaliacoes
            .AsNoTracking()
            .Where(a => ids.Contains(a.AvaliadoId))
            .Select(a => new { a.AvaliadoId, a.Classificacao })
            .ToListAsync(ct);

        var medias = classificacoes
            .GroupBy(a => a.AvaliadoId)
            .ToDictionary(
                g => g.Key,
                g => Math.Round(g.Average(a => (decimal)a.Classificacao), 1, MidpointRounding.AwayFromZero));

        return nomes.ToDictionary(
            u => u.Id,
            u => new CondutorViagem(u.Id, u.Nome, medias.TryGetValue(u.Id, out var media) ? media : null));
    }
}
