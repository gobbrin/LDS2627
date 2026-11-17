using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Participacoes;

namespace CampusCarpool.Application.Viagens;

/// <summary>Viagens (F002): criar, consultar, listar e editar.</summary>
/// <remarks>
/// As datas têm de vir em UTC (<c>Kind = Utc</c>, ou seja, com <c>Z</c> no JSON); offsets como <c>+01:00</c> são
/// recusados (DC-42). Em SQL Server, a coluna <c>Versao</c> faz falhar a segunda de duas gravações simultâneas da
/// mesma viagem, o que chega ao cliente como 409 (<see cref="Domain.Exceptions.RegraDeNegocioException"/>). A
/// simulação da alteração está em <see cref="ISimulacaoDeAlteracaoService"/> e indisponibilizar e concluir em
/// <see cref="ICicloDeVidaDaViagemService"/>.
/// </remarks>
public interface IViagemService
{
    /// <summary>Cria uma viagem <c>Ativa</c> com o utilizador como condutor (RF-F002-01 a RF-F002-06).</summary>
    /// <remarks>
    /// Geocodifica a origem e o destino e calcula a rota no Serviço de Mapas (RF-F008-01, RF-F008-02); sem ele a
    /// criação é recusada (RF-F002-17, P-19). Guarda as coordenadas, a distância, a duração e a rota.
    /// </remarks>
    /// <returns>
    /// A viagem; erro 400 com dados inválidos (vazios, mais de 200 caracteres, origem e destino no mesmo local, data
    /// passada ou não UTC, lugares fora de 1 a 8); 422 se um local ou a rota não forem
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

    /// <summary>Uma página das viagens ativas do condutor, por ordem de partida, para as gerir (RF-F002-15, DEF-BE-010, D-4).</summary>
    /// <remarks>As viagens noutros estados estão no histórico (RF-F009-02).</remarks>
    /// <param name="condutorId">Condutor autenticado.</param>
    /// <param name="pagina">Página, a contar de 1 (validada pelo controller).</param>
    /// <param name="tamanho">Viagens por página, de 1 a 100 (validado pelo controller).</param>
    /// <param name="ct">Cancelamento.</param>
    /// <returns>A página, com o total de viagens ativas.</returns>
    Task<PaginaViagensDoCondutor> ListarDoCondutorAsync(Guid condutorId, int pagina, int tamanho, CancellationToken ct);

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
    /// Sucesso; erro 400 com dados inválidos (locais vazios, com mais de 200 caracteres ou no mesmo local, data passada
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
}
