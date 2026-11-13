using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Exceptions;

namespace CampusCarpool.Domain.Entities;

/// <summary>
/// Pedido de um passageiro para participar numa viagem. Aplica as transições de AD-001 §5 e o limite de P-03.
/// </summary>
/// <remarks>
/// O estado inicial é <see cref="EstadoParticipacao.Pendente"/>.
/// Apenas participações <see cref="EstadoParticipacao.Aceite"/> ocupam lugares.
/// O estado é guardado como texto. Só muda por estes métodos: o <c>set</c> é interno ao Domain (e aberto aos testes,
/// para montar cenários). Uma transição inválida lança <see cref="RegraDeNegocioException"/>.
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

    /// <summary>Estado do pedido.</summary>
    public EstadoParticipacao Estado { get; internal set; } = EstadoParticipacao.Pendente;

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
    public DateTime? AceiteEm { get; internal set; }

    /// <summary>
    /// Versão da linha para concorrência otimista (<c>rowversion</c> no SQL Server; ignorada em SQLite). Aceitar e
    /// cancelar o mesmo pedido em simultâneo fazem falhar a segunda gravação (DO-03).
    /// </summary>
    public byte[] Versao { get; set; } = Array.Empty<byte>();

    /// <summary>O pedido aguarda a decisão do condutor.</summary>
    public bool EstaPendente => Estado == EstadoParticipacao.Pendente;

    /// <summary>O pedido foi aceite e ocupa um lugar.</summary>
    public bool EstaAceite => Estado == EstadoParticipacao.Aceite;

    /// <summary>Aceita um pedido pendente (RF-F004-10).</summary>
    /// <param name="agora">Momento da aceitação (UTC).</param>
    /// <exception cref="RegraDeNegocioException">O pedido não está pendente.</exception>
    public void Aceitar(DateTime agora)
    {
        GarantirPendente();

        Estado = EstadoParticipacao.Aceite;
        AceiteEm = agora;
    }

    /// <summary>Rejeita um pedido pendente (RF-F004-12).</summary>
    /// <exception cref="RegraDeNegocioException">O pedido não está pendente.</exception>
    public void Rejeitar()
    {
        GarantirPendente();

        Estado = EstadoParticipacao.Rejeitada;
    }

    /// <summary>
    /// Indica se o passageiro pode cancelar: um pedido pendente, em qualquer altura, ou uma participação aceite, só até à
    /// hora de partida (RF-F004-15, RF-F004-16, P-03).
    /// </summary>
    /// <param name="viagem">Viagem da participação.</param>
    /// <param name="agora">Momento de referência (UTC).</param>
    public bool PodeSerCanceladaPeloPassageiro(Viagem viagem, DateTime agora)
    {
        ArgumentNullException.ThrowIfNull(viagem);

        return EstaPendente || (EstaAceite && viagem.PartidaEm > agora);
    }

    /// <summary>Cancela a participação a pedido do passageiro.</summary>
    /// <remarks>Libertar o lugar de uma participação aceite é da viagem (<see cref="Viagem.LibertarLugar"/>).</remarks>
    /// <param name="viagem">Viagem da participação.</param>
    /// <param name="agora">Momento de referência (UTC).</param>
    /// <exception cref="RegraDeNegocioException">O estado ou a hora não o permitem.</exception>
    public void CancelarPeloPassageiro(Viagem viagem, DateTime agora)
    {
        if (!PodeSerCanceladaPeloPassageiro(viagem, agora))
            throw new RegraDeNegocioException("A participação já não pode ser cancelada.");

        Estado = EstadoParticipacao.Cancelada;
    }

    /// <summary>Cancela um pedido pendente ou uma participação aceite de uma viagem indisponibilizada (P-25, RF-F002-10).</summary>
    /// <exception cref="RegraDeNegocioException">A participação já estava decidida ou cancelada.</exception>
    public void CancelarPorIndisponibilidade()
    {
        if (!EstaPendente && !EstaAceite)
            throw new RegraDeNegocioException("A participação já não está ativa.");

        Estado = EstadoParticipacao.Cancelada;
    }

    /// <summary>
    /// Cancela um pedido pendente de uma viagem concluída (RF-F004-19). As participações aceites mantêm-se, para o
    /// histórico e a avaliação (P-16).
    /// </summary>
    /// <exception cref="RegraDeNegocioException">O pedido não está pendente.</exception>
    public void CancelarPorConclusao()
    {
        GarantirPendente();

        Estado = EstadoParticipacao.Cancelada;
    }

    private void GarantirPendente()
    {
        if (!EstaPendente)
            throw new RegraDeNegocioException("O pedido já não está pendente.");
    }
}
