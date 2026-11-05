using System.Text.Json.Serialization;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Domain.Entities;

/// <summary>
/// Pedido de um passageiro para participar numa viagem.
/// </summary>
/// <remarks>
/// O estado inicial é <see cref="EstadoParticipacao.Pendente"/>.
/// Apenas participações <see cref="EstadoParticipacao.Aceite"/> ocupam lugares.
/// A conversão EF mantém o estado persistido como texto para compatibilidade
/// com o esquema existente.
/// As transições de estado (aceitar, rejeitar, cancelar) estão em <c>ParticipacaoService</c>, e a
/// indisponibilização em <c>ViagensController</c>; não nesta entidade (DO-01).
/// Ao contrário da <see cref="Viagem"/>, não tem coluna de versão: aceitar e cancelar o mesmo pedido em
/// simultâneo não gera conflito (DO-03, fica para m4).
/// Os pontos indicados no pedido ficam com o texto e as coordenadas obtidas na validação (A8); um ponto omitido
/// fica a <c>null</c> e segue a origem ou o destino da viagem, mesmo que o condutor os altere (P-20).
/// </remarks>
public sealed class Participacao
{
    /// <summary>Identificador da participação.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Viagem pedida.</summary>
    public Guid ViagemId { get; set; }

    /// <summary>Navegação para a viagem.</summary>
    public Viagem Viagem { get; set; } = null!;

    /// <summary>Passageiro que fez o pedido.</summary>
    public Guid PassageiroId { get; set; }

    /// <summary>Navegação para o passageiro.</summary>
    public Utilizador Passageiro { get; set; } = null!;

    /// <summary>Estado do pedido; serializado em JSON como texto (<c>"Pendente"</c>, <c>"Aceite"</c>, ...).</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))] public EstadoParticipacao Estado { get; set; } = EstadoParticipacao.Pendente;

    /// <summary>Ponto de recolha indicado pelo passageiro (texto); <c>null</c> significa a origem da viagem (P-20).</summary>
    public string? Recolha { get; set; }

    /// <summary>Ponto de destino indicado pelo passageiro (texto); <c>null</c> significa o destino da viagem (P-20).</summary>
    public string? Destino { get; set; }

    /// <summary>Latitude da recolha; <c>null</c> se o ponto foi omitido e segue a origem da viagem (P-20).</summary>
    public decimal? RecolhaLatitude { get; set; }

    /// <summary>Longitude da recolha.</summary>
    public decimal? RecolhaLongitude { get; set; }

    /// <summary>Latitude do destino; <c>null</c> se o ponto foi omitido e segue o destino da viagem (P-20).</summary>
    public decimal? DestinoLatitude { get; set; }

    /// <summary>Longitude do destino.</summary>
    public decimal? DestinoLongitude { get; set; }

    /// <summary>Data do pedido (UTC).</summary>
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    /// <summary>Momento em que o condutor aceitou o pedido; fica preenchido mesmo depois de a participação ser cancelada.</summary>
    /// <remarks>Mantém no histórico do passageiro as viagens indisponibilizadas depois da aceitação (RF-F009-08).</remarks>
    public DateTime? AceiteEm { get; set; }
}
