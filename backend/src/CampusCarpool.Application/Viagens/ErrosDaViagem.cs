using CampusCarpool.Application.Comum;

namespace CampusCarpool.Application.Viagens;

/// <summary>
/// Erros comuns às operações sobre uma viagem (F002), num só sítio para as mensagens serem iguais em todas.
/// </summary>
/// <remarks>
/// Usados por <see cref="ViagemService"/>, <see cref="SimulacaoDeAlteracaoService"/>,
/// <see cref="CicloDeVidaDaViagemService"/> e <see cref="AlteracaoDeViagem"/> (AE-01, AE-02).
/// </remarks>
internal static class ErrosDaViagem
{
    /// <summary>Mensagem do 502: o Serviço de Mapas respondeu com erro.</summary>
    public const string MapasIndisponiveis = "Serviço de mapas indisponível.";

    /// <summary>Mensagem do 504: o Serviço de Mapas passou o limite de 10 segundos (P-19).</summary>
    public const string TempoExcedido = "Tempo de resposta do serviço de mapas excedido.";

    /// <summary>404: a viagem não existe.</summary>
    public static readonly Erro NaoEncontrada = new(TipoErro.NaoEncontrado, "Viagem não encontrada.");

    /// <summary>403: o utilizador não é o condutor da viagem.</summary>
    public static readonly Erro Proibido = new(TipoErro.Proibido, "Não tens permissão para gerir esta viagem.");

    /// <summary>422: a origem ou o destino não foram encontrados pelo Serviço de Mapas.</summary>
    public static readonly Erro LocalNaoEncontrado = new(TipoErro.NaoProcessavel, "Origem ou destino não encontrado.");

    /// <summary>422: o Serviço de Mapas não devolveu rota.</summary>
    public static readonly Erro RotaNaoCalculada = new(TipoErro.NaoProcessavel, "Não foi possível calcular a rota.");

    /// <summary>400: a origem e o destino são o mesmo local (RF-F002-04).</summary>
    public static readonly Erro MesmoLocal = new(TipoErro.DadosInvalidos, "A origem e o destino não podem ser o mesmo local.");
}
