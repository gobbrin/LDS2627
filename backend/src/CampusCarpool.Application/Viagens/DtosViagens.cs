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

/// <summary>Viagem nos resultados da pesquisa, com o condutor e os pontos adicionais (RF-F003-03, RF-F003-04).</summary>
public sealed record ViagemEncontrada(
    Guid Id,
    string Origem,
    string Destino,
    DateTime PartidaEm,
    int LugaresDisponiveis,
    decimal? DistanciaKm,
    int? DuracaoMinutos,
    int PontosAdicionais,
    CondutorViagem? Condutor);

/// <summary>Página de resultados da pesquisa.</summary>
public sealed record ResultadoPesquisa(int Total, int Pagina, int Tamanho, IReadOnlyList<ViagemEncontrada> Viagens);

/// <summary>Dados públicos de um condutor nos resultados e nos detalhes de uma viagem (RF-F003-04).</summary>
public sealed record CondutorViagem(Guid Id, string Nome, decimal? ClassificacaoMedia);

/// <summary>Viagem ativa na lista do próprio condutor.</summary>
public sealed record ViagemDoCondutor(
    Guid Id,
    string Origem,
    string Destino,
    DateTime PartidaEm,
    int LugaresOferecidos,
    int LugaresDisponiveis);

/// <summary>Filtros da pesquisa de viagens. Os nulos não filtram.</summary>
/// <param name="Origem">Texto contido na origem, sem distinguir maiúsculas nem acentos.</param>
/// <param name="Destino">Texto contido no destino, nas mesmas condições.</param>
/// <param name="Data">Dia da partida.</param>
/// <param name="Hora">Hora mínima da partida.</param>
/// <param name="Pagina">Página, a contar de 1.</param>
/// <param name="Tamanho">Viagens por página.</param>
public sealed record FiltroPesquisaViagens(
    string? Origem,
    string? Destino,
    DateOnly? Data,
    TimeOnly? Hora,
    int Pagina,
    int Tamanho);
