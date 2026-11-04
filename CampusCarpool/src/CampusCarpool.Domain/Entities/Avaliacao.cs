namespace CampusCarpool.Domain.Entities;

/// <summary>
/// Classificação (1 a 5) que um participante atribui a outro participante de uma viagem concluída (F007).
/// </summary>
/// <remarks>
/// As regras de elegibilidade (P-16: avaliador e avaliado participaram na mesma viagem <c>Concluida</c>;
/// ninguém se avalia a si próprio) estão em <c>AvaliacaoService</c>, não nesta entidade.
/// RF-F007-04 exige uma só avaliação atual por par (avaliador, avaliado): a migration
/// <c>I048_AvaliacoesPorPar</c> cria esse índice único, mas o <c>CampusCarpoolDbContext</c> e o snapshot
/// ainda declaram o índice triplo (viagem, avaliador, avaliado) (IN-01 / DEF-BE-025, fica para m4).
/// O autor (<see cref="AvaliadorId"/>) nunca é exposto a outros utilizadores (RF-F007-07).
/// </remarks>
public sealed class Avaliacao
{
    /// <summary>Identificador da avaliação.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Viagem concluída que tornou a avaliação possível.</summary>
    public Guid ViagemId { get; set; }

    /// <summary>Utilizador que avalia (autor); não é revelado a terceiros.</summary>
    public Guid AvaliadorId { get; set; }

    /// <summary>Utilizador avaliado.</summary>
    public Guid AvaliadoId { get; set; }

    /// <summary>Valor da classificação, de 1 a 5 (validado no serviço).</summary>
    public byte Classificacao { get; set; }

    /// <summary>Data de criação (UTC). Alterar a avaliação não atualiza esta data (DO-10).</summary>
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
