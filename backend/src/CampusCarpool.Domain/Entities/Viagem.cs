using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Exceptions;

namespace CampusCarpool.Domain.Entities;

/// <summary>
/// Viagem oferecida por um condutor (F002). Aplica os estados de AD-001 e é o ponto único de alteração de lugares.
/// </summary>
/// <remarks>
/// Ciclo de vida (P-04): <c>Ativa</c> → <c>Indisponibilizada</c> (pelo condutor, RF-F002-09) ou
/// <c>Concluida</c> (manualmente pelo condutor, ou automaticamente pelo <c>TrabalhadorConclusaoViagens</c>
/// depois da hora de partida mais a duração estimada, RF-F002-12). As viagens nunca são apagadas (RF-F002-14).
/// O estado é guardado como texto, com o nome do membro de <see cref="EstadoViagem"/>, e a restrição
/// <c>CK_Viagens_Estado</c> recusa outros valores.
/// Os dados que definem o estado e os lugares só mudam por estes métodos: os <c>set</c> são internos ao Domain
/// (e abertos aos testes, para montar cenários). Uma transição inválida lança <see cref="RegraDeNegocioException"/>;
/// os serviços verificam antes, para devolverem o erro adequado a cada caso.
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
    public string Origem { get; internal set; } = "";

    /// <summary>Local de destino (texto pesquisável).</summary>
    public string Destino { get; internal set; } = "";

    /// <summary>Origem sem acentos e em maiúsculas, para a pesquisa (P-12). Preenchida pelo <c>CampusCarpoolDbContext</c> ao gravar.</summary>
    public string OrigemPesquisa { get; set; } = "";

    /// <summary>Destino sem acentos e em maiúsculas, para a pesquisa (P-12). Preenchido pelo <c>CampusCarpoolDbContext</c> ao gravar.</summary>
    public string DestinoPesquisa { get; set; } = "";

    /// <summary>Latitude da origem, obtida pelo Serviço de Mapas (RF-F008-01). As quatro coordenadas são obrigatórias (A8).</summary>
    public decimal OrigemLatitude { get; internal set; }

    /// <summary>Longitude da origem.</summary>
    public decimal OrigemLongitude { get; internal set; }

    /// <summary>Latitude do destino.</summary>
    public decimal DestinoLatitude { get; internal set; }

    /// <summary>Longitude do destino.</summary>
    public decimal DestinoLongitude { get; internal set; }

    /// <summary>
    /// Data e hora de partida (UTC). A API só aceita valores com <c>Kind = Utc</c>.
    /// </summary>
    public DateTime PartidaEm { get; internal set; }

    /// <summary>Minuto do dia da partida na hora de Portugal, para a pesquisa (P-12). Preenchido pelo <c>CampusCarpoolDbContext</c> ao gravar.</summary>
    public int PartidaMinutoDoDia { get; set; }

    /// <summary>Número de lugares oferecidos, de 1 a 8 (P-11).</summary>
    public int LugaresOferecidos { get; internal set; }

    /// <summary>Lugares ainda livres: oferecidos menos participações aceites (RF-F002-13).</summary>
    public int LugaresDisponiveis { get; internal set; }

    /// <summary>Estado da viagem.</summary>
    public EstadoViagem Estado { get; internal set; } = EstadoViagem.Ativa;

    /// <summary>Distância da rota atual, em km (RF-F008-02); base da estimativa de custo (P-06).</summary>
    public decimal? DistanciaKm { get; internal set; }

    /// <summary>
    /// Duração estimada da última criação ou alteração da viagem, em minutos; aceitar ou cancelar não a muda (P-04).
    /// Usada na conclusão automática; sem ela, a viagem conclui-se à hora de partida.
    /// </summary>
    public int? DuracaoMinutos { get; internal set; }

    /// <summary>
    /// Rota atual em JSON: pontos por ordem, com os das participações aceites (RF-F008-05, modelo de dados:
    /// <c>rota_pontos</c>). <c>null</c> significa «por recalcular» (P-19).
    /// </summary>
    public string? RotaPontos { get; internal set; }

    /// <summary>
    /// Versão da linha para concorrência otimista (<c>rowversion</c> no SQL Server; ignorada em SQLite).
    /// Se duas gravações da mesma viagem se cruzarem, a segunda falha com <c>DbUpdateConcurrencyException</c>.
    /// </summary>
    public byte[] Versao { get; set; } = Array.Empty<byte>();

    /// <summary>A viagem está no estado <c>Ativa</c>.</summary>
    public bool EstaAtiva => Estado == EstadoViagem.Ativa;

    /// <summary>A viagem foi concluída.</summary>
    public bool EstaConcluida => Estado == EstadoViagem.Concluida;

    /// <summary>Lugares ocupados pelas participações aceites (RF-F002-13).</summary>
    public int LugaresOcupados => LugaresOferecidos - LugaresDisponiveis;

    /// <summary>Cria uma viagem <c>Ativa</c>, com todos os lugares livres (RF-F002-01, RF-F002-05).</summary>
    /// <param name="condutorId">Condutor.</param>
    /// <param name="locais">Origem e destino, com as coordenadas.</param>
    /// <param name="partidaEm">Data e hora de partida (UTC).</param>
    /// <param name="lugares">Lugares oferecidos, de 1 a 8 (P-11).</param>
    /// <param name="duracaoMinutos">Duração estimada (P-04).</param>
    /// <returns>A viagem, por gravar e ainda sem rota.</returns>
    public static Viagem Criar(Guid condutorId, LocaisViagem locais, DateTime partidaEm, int lugares, int? duracaoMinutos)
    {
        var viagem = new Viagem
        {
            CondutorId = condutorId,
            PartidaEm = partidaEm,
            LugaresOferecidos = lugares,
            LugaresDisponiveis = lugares,
            DuracaoMinutos = duracaoMinutos
        };

        viagem.DefinirLocais(locais);

        return viagem;
    }

    /// <summary>Indica se a viagem ainda aceita alterações e pedidos: <c>Ativa</c> e com a partida por chegar.</summary>
    /// <param name="agora">Momento de referência (UTC).</param>
    public bool PorPartir(DateTime agora) => EstaAtiva && PartidaEm > agora;

    /// <summary>Altera os dados de uma viagem ativa ainda por partir (RF-F002-07, RF-F002-08).</summary>
    /// <remarks>Os lugares não podem descer abaixo dos ocupados; os disponíveis são recalculados.</remarks>
    /// <param name="locais">Origem e destino.</param>
    /// <param name="partidaEm">Nova data e hora de partida (UTC).</param>
    /// <param name="lugares">Lugares oferecidos.</param>
    /// <param name="duracaoMinutos">Nova duração estimada (P-04).</param>
    /// <param name="agora">Momento de referência (UTC).</param>
    /// <exception cref="RegraDeNegocioException">A viagem já não pode ser alterada ou os lugares não chegam.</exception>
    public void Alterar(LocaisViagem locais, DateTime partidaEm, int lugares, int? duracaoMinutos, DateTime agora)
    {
        ArgumentNullException.ThrowIfNull(locais);

        if (!PorPartir(agora))
            throw new RegraDeNegocioException("A viagem já não pode ser alterada.");

        if (lugares < LugaresOcupados)
            throw new RegraDeNegocioException("Os lugares não podem ser inferiores às participações aceites.");

        var ocupados = LugaresOcupados;

        DefinirLocais(locais);
        PartidaEm = partidaEm;
        DuracaoMinutos = duracaoMinutos;
        LugaresOferecidos = lugares;
        LugaresDisponiveis = lugares - ocupados;
    }

    /// <summary>Indisponibiliza a viagem e liberta todos os lugares (RF-F002-09, P-25).</summary>
    /// <remarks>Permitido mesmo depois da hora de partida, desde que ainda esteja <c>Ativa</c>.</remarks>
    /// <exception cref="RegraDeNegocioException">A viagem já não está ativa.</exception>
    public void Indisponibilizar()
    {
        if (!EstaAtiva)
            throw new RegraDeNegocioException("Só uma viagem ativa pode ser indisponibilizada.");

        Estado = EstadoViagem.Indisponibilizada;
        LugaresDisponiveis = LugaresOferecidos;
    }

    /// <summary>Indica se a viagem pode ser concluída pelo condutor: <c>Ativa</c> e depois da hora de partida.</summary>
    /// <param name="agora">Momento de referência (UTC).</param>
    public bool PodeConcluir(DateTime agora) => EstaAtiva && PartidaEm <= agora;

    /// <summary>
    /// Indica se a conclusão automática se aplica: <c>Ativa</c> e terminada a duração estimada (P-04, RF-F002-12).
    /// Sem duração estimada, conclui-se à hora de partida.
    /// </summary>
    /// <param name="agora">Momento de referência (UTC).</param>
    public bool DeveConcluir(DateTime agora) => EstaAtiva && PartidaEm.AddMinutes(DuracaoMinutos ?? 0) <= agora;

    /// <summary>Conclui a viagem (RF-F002-12).</summary>
    /// <param name="agora">Momento de referência (UTC).</param>
    /// <exception cref="RegraDeNegocioException">A viagem não está ativa ou ainda não partiu.</exception>
    public void Concluir(DateTime agora)
    {
        if (!PodeConcluir(agora))
            throw new RegraDeNegocioException("Só uma viagem ativa e já partida pode ser concluída.");

        Estado = EstadoViagem.Concluida;
    }

    /// <summary>Ocupa um lugar, ao aceitar uma participação.</summary>
    /// <exception cref="RegraDeNegocioException">Não há lugares disponíveis.</exception>
    public void OcuparLugar()
    {
        if (LugaresDisponiveis <= 0)
            throw new RegraDeNegocioException("Sem lugares disponíveis.");

        LugaresDisponiveis--;
    }

    /// <summary>Liberta um lugar, ao cancelar uma participação aceite (RF-F004-17).</summary>
    /// <exception cref="RegraDeNegocioException">Todos os lugares já estão livres.</exception>
    public void LibertarLugar()
    {
        if (LugaresDisponiveis >= LugaresOferecidos)
            throw new RegraDeNegocioException("Todos os lugares já estão disponíveis.");

        LugaresDisponiveis++;
    }

    /// <summary>Indica se os lugares guardados batem com a contagem real de aceites (RF-F002-13).</summary>
    /// <param name="aceites">Número de participações aceites.</param>
    public bool LugaresConsistentes(int aceites) =>
        LugaresDisponiveis >= 0 && LugaresOferecidos - aceites == LugaresDisponiveis;

    /// <summary>Guarda a rota atual e a respetiva distância. A duração de P-04 não muda (RF-F008-05).</summary>
    /// <param name="rotaPontos">Rota, no formato da coluna <see cref="RotaPontos"/>.</param>
    /// <param name="distanciaKm">Distância total, em km.</param>
    public void AtualizarRota(string rotaPontos, decimal distanciaKm)
    {
        RotaPontos = rotaPontos;
        DistanciaKm = distanciaKm;
    }

    /// <summary>Marca a rota como por recalcular, quando o Serviço de Mapas não respondeu (P-19).</summary>
    public void MarcarRotaPorRecalcular() => RotaPontos = null;

    /// <summary>Glossário: <c>Ativa</c>, com data e hora futuras e pelo menos um lugar disponível (RF-F003-02).</summary>
    /// <param name="agora">Momento de referência (UTC).</param>
    public bool EstaDisponivelParaPesquisa(DateTime agora) => PorPartir(agora) && LugaresDisponiveis > 0;

    private void DefinirLocais(LocaisViagem locais)
    {
        Origem = locais.Origem;
        Destino = locais.Destino;
        OrigemLatitude = locais.CoordenadasOrigem.Latitude;
        OrigemLongitude = locais.CoordenadasOrigem.Longitude;
        DestinoLatitude = locais.CoordenadasDestino.Latitude;
        DestinoLongitude = locais.CoordenadasDestino.Longitude;
    }
}
