using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Rotas;

namespace CampusCarpool.Application.Viagens;

/// <summary>Viagem acabada de criar.</summary>
public sealed record ViagemCriada(
    Guid Id,
    string Origem,
    string Destino,
    DateTime PartidaEm,
    int LugaresDisponiveis,
    decimal? DistanciaKm,
    int? DuracaoMinutos);

/// <summary>Detalhes de uma viagem, com a estimativa de custo por participante.</summary>
/// <param name="Id">Identificador.</param>
/// <param name="Origem">Origem.</param>
/// <param name="Destino">Destino.</param>
/// <param name="PartidaEm">Data e hora de partida.</param>
/// <param name="LugaresDisponiveis">Lugares livres.</param>
/// <param name="Estado">Estado da viagem.</param>
/// <param name="DistanciaKm">Distância da rota atual.</param>
/// <param name="DuracaoMinutos">Duração da rota atual.</param>
/// <param name="CondutorId">Condutor.</param>
/// <param name="Condutor">Nome e classificação média do condutor.</param>
/// <param name="PontosAdicionais">Número de pontos adicionais da rota.</param>
/// <param name="EstimativaCusto">Estimativa; <c>null</c> se não for possível calculá-la.</param>
/// <param name="EstimativaIndisponivelMotivo">Motivo de não haver estimativa (RF-F008-12).</param>
public sealed record DetalhesViagem(
    Guid Id,
    string Origem,
    string Destino,
    DateTime PartidaEm,
    int LugaresDisponiveis,
    string Estado,
    decimal? DistanciaKm,
    int? DuracaoMinutos,
    Guid CondutorId,
    CondutorViagem? Condutor,
    int PontosAdicionais,
    ResultadoEstimativaCusto? EstimativaCusto,
    string? EstimativaIndisponivelMotivo);

/// <summary>Ciclo de vida das viagens (F002): criar, consultar, editar, indisponibilizar e concluir.</summary>
/// <remarks>
/// As datas têm de vir em UTC (<c>Kind = Utc</c>, ou seja, com <c>Z</c> no JSON); offsets como <c>+01:00</c> são
/// recusados (DC-42). Em SQL Server, a coluna <c>Versao</c> faz falhar a segunda de duas gravações simultâneas da
/// mesma viagem, o que chega ao cliente como 409 (<see cref="Domain.Exceptions.RegraDeNegocioException"/>).
/// </remarks>
public interface IViagemService
{
    /// <summary>Cria uma viagem <c>Ativa</c> com o utilizador como condutor (RF-F002-01 a RF-F002-06).</summary>
    /// <remarks>
    /// Geocodifica a origem e o destino e calcula a rota no Serviço de Mapas (RF-F008-01, RF-F008-02); sem ele a
    /// criação é recusada (RF-F002-17, P-19). Guarda as coordenadas, a distância, a duração e a rota.
    /// </remarks>
    /// <returns>
    /// A viagem; erro 400 com dados inválidos (vazios, mais de 250 caracteres, origem e destino no mesmo local, data
    /// passada ou não UTC, lugares fora de 1 a 8); 403 com a conta por confirmar; 422 se um local ou a rota não forem
    /// encontrados; 502/503/504 com problemas no Serviço de Mapas.
    /// </returns>
    Task<Resultado<ViagemCriada>> CriarAsync(
        Guid condutorId,
        string origem,
        string destino,
        DateTime partidaEm,
        int lugares,
        CancellationToken ct);

    /// <summary>Detalhes de uma viagem com a estimativa de custo por participante (RF-F003-06, RF-F008-08, RF-F008-10).</summary>
    /// <remarks>
    /// Outro utilizador só consulta uma viagem pesquisável; o condutor e quem tem pedido nela consultam-na sempre
    /// (AD-003 §5.1, A7). A distância, a duração e a estimativa vêm da rota guardada (sem expor os pontos,
    /// RF-F003-08); uma rota por recalcular é recalculada aqui (P-19). Se não for possível, devolve
    /// <c>EstimativaCusto = null</c> e o motivo em <c>EstimativaIndisponivelMotivo</c> (RF-F008-12), em vez de falhar.
    /// </remarks>
    /// <returns>Os detalhes, ou erro 404 se a viagem não existir ou o utilizador não a puder consultar.</returns>
    Task<Resultado<DetalhesViagem>> ConsultarAsync(Guid viagemId, Guid? utilizadorId, CancellationToken ct);

    /// <summary>Viagens ativas do condutor, por ordem de partida, para as gerir (RF-F002-15, DEF-BE-010).</summary>
    /// <remarks>As viagens noutros estados estão no histórico (RF-F009-02).</remarks>
    Task<IReadOnlyList<ViagemDoCondutor>> ListarDoCondutorAsync(Guid condutorId, CancellationToken ct);

    /// <summary>Passageiros aceites de uma viagem, com os respetivos pontos (RF-F003-07, P-13, DEF-BE-012).</summary>
    /// <remarks>Um ponto a <c>null</c> segue a origem ou o destino da viagem (P-20).</remarks>
    /// <returns>A lista; erro 403 se o utilizador não for o condutor nem passageiro aceite; 404 se a viagem não existir.</returns>
    Task<Resultado<List<PassageiroAceite>>> ListarPassageirosAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct);

    /// <summary>Pedidos pendentes de uma viagem, do mais antigo para o mais recente (RF-F004-08, DEF-BE-016).</summary>
    /// <returns>A lista; erro 403 se o utilizador não for o condutor; 404 se a viagem não existir.</returns>
    Task<Resultado<List<PedidoPendente>>> ListarPedidosPendentesAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct);

    /// <summary>Altera a origem, o destino, a data/hora e os lugares de uma viagem ativa ainda por partir (RF-F002-07, RF-F002-08, RF-F002-16).</summary>
    /// <remarks>
    /// <paramref name="origem"/> e <paramref name="destino"/> a <c>null</c> mantêm o valor atual. A alteração depende
    /// sempre do Serviço de Mapas (RF-F002-17, P-19): recalcula a rota e guarda a nova duração, a de P-04. Um ponto
    /// omitido num pedido segue a nova origem ou o novo destino (P-20). Os lugares não podem descer abaixo dos já
    /// ocupados; os disponíveis são recalculados. Notifica os passageiros aceites (Alteração da viagem, RF-F006-04),
    /// que decidem se ficam.
    /// </remarks>
    /// <returns>
    /// Sucesso; erro 400 com dados inválidos (locais vazios, com mais de 250 caracteres ou no mesmo local, data passada
    /// ou não UTC, lugares fora de 1 a 8); 403 se não for o condutor; 404 se a viagem não existir; 409 com a viagem não
    /// editável ou lugares abaixo dos ocupados; 422 se um local ou a rota não forem encontrados; 502/503/504 com
    /// problemas no Serviço de Mapas.
    /// </returns>
    Task<Resultado> EditarAsync(
        Guid viagemId,
        Guid utilizadorId,
        DateTime partidaEm,
        int lugares,
        string? origem,
        string? destino,
        CancellationToken ct);

    /// <summary>Indisponibiliza uma viagem ativa (RF-F002-09, RF-F002-10, P-25).</summary>
    /// <remarks>
    /// Cancela os pedidos pendentes e as participações aceites, liberta todos os lugares e notifica os passageiros
    /// afetados, não o condutor (P-23, RF-F006-03). A viagem fica no histórico dos aceites graças a <c>AceiteEm</c>.
    /// Permitido mesmo depois da hora de partida, desde que ainda esteja <c>Ativa</c>. Fica sem os pontos na rota; o
    /// Serviço de Mapas não impede a operação (P-19).
    /// </remarks>
    /// <returns>Sucesso; erro 403 se não for o condutor; 404 se a viagem não existir; 409 se já não estiver ativa.</returns>
    Task<Resultado> IndisponibilizarAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct);

    /// <summary>Conclusão manual pelo condutor, depois da hora de partida.</summary>
    /// <remarks>
    /// Faz o mesmo que a conclusão automática (<see cref="IConclusaoViagensService"/>, RF-F002-12): passa a
    /// <c>Concluida</c> e cancela os pendentes (RF-F004-19); os aceites mantêm-se para a avaliação (P-16). Não envia
    /// notificações. Mantida até o grupo decidir PA-01/PA-03.
    /// </remarks>
    /// <returns>Sucesso; erro 403 se não for o condutor; 404 se a viagem não existir; 409 se não estiver ativa ou ainda não tiver partido.</returns>
    Task<Resultado> ConcluirAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct);
}
