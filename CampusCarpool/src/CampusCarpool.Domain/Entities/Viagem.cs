using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Domain.Entities;

/// <summary>
/// Viagem oferecida por um condutor (F002).
/// </summary>
/// <remarks>
/// Ciclo de vida (P-04): <c>Ativa</c> → <c>Indisponibilizada</c> (pelo condutor, RF-F002-09) ou
/// <c>Concluida</c> (manualmente pelo condutor, ou automaticamente pelo <c>TrabalhadorConclusaoViagens</c>
/// depois da hora de partida mais a duração estimada, RF-F002-12). As viagens nunca são apagadas (RF-F002-14).
/// O estado é guardado como texto, mas só aceita os nomes de <see cref="EstadoViagem"/>: o código escreve-os
/// sempre com <c>nameof</c> e a restrição <c>CK_Viagens_Estado</c> recusa outros valores.
/// As transições estão em <c>ViagensController</c> e nos serviços, não nesta entidade (DO-01).
/// </remarks>
public sealed class Viagem
{
    /// <summary>Identificador da viagem.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Utilizador que criou a viagem (RF-F002-05).</summary>
    public Guid CondutorId { get; set; }

    /// <summary>Navegação para o condutor.</summary>
    public Utilizador Condutor { get; set; } = null!;

    /// <summary>Local de origem (texto pesquisável).</summary>
    public string Origem { get; set; } = "";

    /// <summary>Local de destino (texto pesquisável).</summary>
    public string Destino { get; set; } = "";

    /// <summary>Latitude da origem, obtida pelo Serviço de Mapas (RF-F008-01). As quatro coordenadas são obrigatórias (A8).</summary>
    public decimal OrigemLatitude { get; set; }

    /// <summary>Longitude da origem.</summary>
    public decimal OrigemLongitude { get; set; }

    /// <summary>Latitude do destino.</summary>
    public decimal DestinoLatitude { get; set; }

    /// <summary>Longitude do destino.</summary>
    public decimal DestinoLongitude { get; set; }

    /// <summary>
    /// Data e hora de partida (UTC). A API só aceita valores com <c>Kind = Utc</c>; lidos da BD vêm com
    /// <c>Kind = Unspecified</c>, porque não há conversor UTC (DC-42, fica para m4).
    /// </summary>
    public DateTime PartidaEm { get; set; }

    /// <summary>Número de lugares oferecidos, de 1 a 8 (P-11).</summary>
    public int LugaresOferecidos { get; set; }

    /// <summary>Lugares ainda livres: oferecidos menos participações aceites (RF-F002-13). Atualizado ao aceitar e ao cancelar.</summary>
    public int LugaresDisponiveis { get; set; }

    /// <summary>Estado da viagem: <c>Ativa</c>, <c>Indisponibilizada</c> ou <c>Concluida</c> (sem acento).</summary>
    public string Estado { get; set; } = nameof(EstadoViagem.Ativa);

    /// <summary>Distância da rota atual, em km (RF-F008-02); base da estimativa de custo (P-06).</summary>
    public decimal? DistanciaKm { get; set; }

    /// <summary>
    /// Duração estimada da última criação ou alteração da viagem, em minutos; aceitar ou cancelar não a muda (P-04).
    /// Usada na conclusão automática; sem ela, a viagem conclui-se à hora de partida.
    /// </summary>
    public int? DuracaoMinutos { get; set; }

    /// <summary>
    /// Rota atual em JSON: pontos por ordem, com os das participações aceites (RF-F008-05, modelo de dados:
    /// <c>rota_pontos</c>). <c>null</c> significa «por recalcular» (P-19).
    /// </summary>
    public string? RotaPontos { get; set; }

    /// <summary>
    /// Versão da linha para concorrência otimista (<c>rowversion</c> no SQL Server; ignorada em SQLite).
    /// Se duas gravações da mesma viagem se cruzarem, a segunda falha com <c>DbUpdateConcurrencyException</c>.
    /// </summary>
    public byte[] Versao { get; set; } = Array.Empty<byte>();
}
