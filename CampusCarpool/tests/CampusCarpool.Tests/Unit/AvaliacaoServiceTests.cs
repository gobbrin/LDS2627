using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Avaliacoes;
using CampusCarpool.Application.Comum;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using Xunit;

namespace CampusCarpool.Tests.Unit;

/// <summary>Regras do <c>AvaliacaoService</c> (F007): valores, auto-avaliação, estado da viagem, elegibilidade (P-16), unicidade do par, alteração e média.</summary>
/// <remarks>
/// Teste unitário: o repositório é um falso em memória, sem base de dados.
/// </remarks>
public sealed class AvaliacaoServiceTests
{
    private readonly Guid _condutor = Guid.NewGuid();
    private readonly Guid _passageiroA = Guid.NewGuid();
    private readonly Guid _passageiroB = Guid.NewGuid();
    private readonly Guid _externo = Guid.NewGuid();
    private readonly Guid _viagemId = Guid.NewGuid();
    private readonly RepositorioEmMemoria _repo = new();

    private AvaliacaoService CriarServico(EstadoViagem estado = EstadoViagem.Concluida, bool aceitarA = true, bool aceitarB = true)
    {
        _repo.Viagens[_viagemId] = new Viagem { Id = _viagemId, CondutorId = _condutor, Estado = estado };
        _repo.Utilizadores.UnionWith(new[] { _condutor, _passageiroA, _passageiroB, _externo });
        if (aceitarA) _repo.Aceites.Add((_viagemId, _passageiroA));
        if (aceitarB) _repo.Aceites.Add((_viagemId, _passageiroB));
        return new AvaliacaoService(_repo);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task Criar_AceitaTodasAsClassificacoesValidas(int valor)
    {
        var servico = CriarServico();
        var resultado = await servico.CriarAsync(_viagemId, _condutor, _passageiroA, valor, CancellationToken.None);
        Assert.Equal(valor, resultado.Valor.Classificacao);
        Assert.Single(_repo.Avaliacoes);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(100)]
    public async Task Criar_RejeitaClassificacaoInvalida(int valor)
    {
        var servico = CriarServico();
        Assert.Equal(TipoErro.DadosInvalidos, (await servico.CriarAsync(_viagemId, _condutor, _passageiroA, valor, CancellationToken.None)).Erro?.Tipo);
        Assert.Empty(_repo.Avaliacoes);
    }

    [Fact]
    public async Task Criar_ImpedeAutoavaliacao()
    {
        var servico = CriarServico();
        Assert.Equal(TipoErro.DadosInvalidos, (await servico.CriarAsync(_viagemId, _condutor, _condutor, 5, CancellationToken.None)).Erro?.Tipo);
    }

    [Theory]
    [InlineData(EstadoViagem.Ativa)]
    [InlineData(EstadoViagem.Indisponibilizada)]
    public async Task Criar_RejeitaViagemNaoConcluida(EstadoViagem estado)
    {
        var servico = CriarServico(estado);
        Assert.Equal(TipoErro.Conflito, (await servico.CriarAsync(_viagemId, _condutor, _passageiroA, 5, CancellationToken.None)).Erro?.Tipo);
    }

    [Fact]
    public async Task Criar_AceitaViagemConcluida()
    {
        var servico = CriarServico(EstadoViagem.Concluida);
        await servico.CriarAsync(_viagemId, _condutor, _passageiroA, 5, CancellationToken.None);
        Assert.Single(_repo.Avaliacoes);
    }

    [Fact]
    public async Task Criar_RejeitaViagemInexistente()
    {
        var servico = CriarServico();
        Assert.Equal(TipoErro.NaoEncontrado, (await servico.CriarAsync(Guid.NewGuid(), _condutor, _passageiroA, 5, CancellationToken.None)).Erro?.Tipo);
    }

    [Fact]
    public async Task Criar_RejeitaUtilizadorInexistente()
    {
        var servico = CriarServico();
        Assert.Equal(TipoErro.NaoEncontrado, (await servico.CriarAsync(_viagemId, _condutor, Guid.NewGuid(), 5, CancellationToken.None)).Erro?.Tipo);
    }

    [Fact]
    public async Task Criar_RejeitaAvaliadorSemParticipacaoAceite()
    {
        var servico = CriarServico();
        Assert.Equal(TipoErro.Conflito, (await servico.CriarAsync(_viagemId, _externo, _condutor, 5, CancellationToken.None)).Erro?.Tipo);
    }

    [Fact]
    public async Task Criar_RejeitaAvaliadoSemParticipacaoAceite()
    {
        var servico = CriarServico(aceitarA: false);
        Assert.Equal(TipoErro.Conflito, (await servico.CriarAsync(_viagemId, _condutor, _passageiroA, 5, CancellationToken.None)).Erro?.Tipo);
    }

    [Fact]
    public async Task Criar_PermiteAvaliacaoEntreDoisPassageirosAceites()
    {
        var servico = CriarServico();
        await servico.CriarAsync(_viagemId, _passageiroA, _passageiroB, 4, CancellationToken.None);
        Assert.Single(_repo.Avaliacoes);
    }

    [Fact]
    public async Task Criar_PermitePassageiroAvaliarCondutor()
    {
        var servico = CriarServico();
        await servico.CriarAsync(_viagemId, _passageiroA, _condutor, 4, CancellationToken.None);
        Assert.Single(_repo.Avaliacoes);
    }

    [Fact]
    public async Task Criar_RejeitaDuplicadoNaMesmaViagem()
    {
        var servico = CriarServico();
        await servico.CriarAsync(_viagemId, _condutor, _passageiroA, 3, CancellationToken.None);
        Assert.Equal(TipoErro.Conflito, (await servico.CriarAsync(_viagemId, _condutor, _passageiroA, 5, CancellationToken.None)).Erro?.Tipo);
        Assert.Single(_repo.Avaliacoes);
    }

    [Fact]
    public async Task Criar_RejeitaMesmoParNoutraViagem()
    {
        var servico = CriarServico();
        await servico.CriarAsync(_viagemId, _condutor, _passageiroA, 3, CancellationToken.None);
        var outra = Guid.NewGuid();
        _repo.Viagens[outra] = new Viagem { Id = outra, CondutorId = _condutor, Estado = EstadoViagem.Concluida };
        _repo.Aceites.Add((outra, _passageiroA));
        Assert.Equal(TipoErro.Conflito, (await servico.CriarAsync(outra, _condutor, _passageiroA, 5, CancellationToken.None)).Erro?.Tipo);
        Assert.Single(_repo.Avaliacoes);
    }

    [Fact]
    public async Task Criar_PermiteAvaliacaoNoSentidoInverso()
    {
        var servico = CriarServico();
        await servico.CriarAsync(_viagemId, _condutor, _passageiroA, 5, CancellationToken.None);
        await servico.CriarAsync(_viagemId, _passageiroA, _condutor, 4, CancellationToken.None);
        Assert.Equal(2, _repo.Avaliacoes.Count);
    }

    [Fact]
    public async Task Alterar_SubstituiValorSemCriarNovaAvaliacao()
    {
        var servico = CriarServico();
        await servico.CriarAsync(_viagemId, _condutor, _passageiroA, 3, CancellationToken.None);
        var resultado = await servico.AlterarAsync(_condutor, _passageiroA, 5, CancellationToken.None);
        Assert.Equal(5, resultado.Valor.Classificacao);
        Assert.Single(_repo.Avaliacoes);
        Assert.Equal(5, _repo.Avaliacoes[0].Classificacao);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-2)]
    public async Task Alterar_RejeitaClassificacaoInvalida(int valor)
    {
        var servico = CriarServico();
        await servico.CriarAsync(_viagemId, _condutor, _passageiroA, 3, CancellationToken.None);
        Assert.Equal(TipoErro.DadosInvalidos, (await servico.AlterarAsync(_condutor, _passageiroA, valor, CancellationToken.None)).Erro?.Tipo);
        Assert.Equal(3, _repo.Avaliacoes[0].Classificacao);
    }

    [Fact]
    public async Task Alterar_RejeitaQuandoNaoExisteAvaliacaoDoAutor()
    {
        var servico = CriarServico();
        Assert.Equal(TipoErro.NaoEncontrado, (await servico.AlterarAsync(_condutor, _passageiroA, 4, CancellationToken.None)).Erro?.Tipo);
    }

    [Fact]
    public async Task Alterar_ImpedeOutroUtilizadorDeAlterarAvaliacao()
    {
        var servico = CriarServico();
        await servico.CriarAsync(_viagemId, _condutor, _passageiroA, 3, CancellationToken.None);
        Assert.Equal(TipoErro.NaoEncontrado, (await servico.AlterarAsync(_passageiroB, _passageiroA, 5, CancellationToken.None)).Erro?.Tipo);
        Assert.Equal(3, _repo.Avaliacoes[0].Classificacao);
    }

    [Fact]
    public async Task Consultar_SemAvaliacoes_DevolveMediaNulaEListaVazia()
    {
        var servico = CriarServico();
        var resumo = (await servico.ConsultarAsync(_condutor, CancellationToken.None)).Valor;
        Assert.Null(resumo.Media);
        Assert.Equal(0, resumo.Total);
        Assert.Empty(resumo.Avaliacoes);
    }

    [Fact]
    public async Task Consultar_CalculaMediaComUmaCasaDecimal()
    {
        var servico = CriarServico();
        await servico.CriarAsync(_viagemId, _condutor, _passageiroA, 5, CancellationToken.None);
        await servico.CriarAsync(_viagemId, _passageiroB, _passageiroA, 4, CancellationToken.None);
        await servico.CriarAsync(_viagemId, _passageiroA, _condutor, 4, CancellationToken.None);
        // A recebe 5 do condutor e 4 do passageiro B: média 4,5.
        var resumo = (await servico.ConsultarAsync(_passageiroA, CancellationToken.None)).Valor;
        Assert.Equal(4.5m, resumo.Media);
        Assert.Equal(2, resumo.Total);
    }

    [Fact]
    public async Task Consultar_ArredondaMediaDeCincoQuatroQuatroParaQuatroVirgulaTres()
    {
        var servico = CriarServico();
        await servico.CriarAsync(_viagemId, _condutor, _passageiroA, 5, CancellationToken.None);
        await servico.CriarAsync(_viagemId, _passageiroB, _passageiroA, 4, CancellationToken.None);
        _repo.Avaliacoes.Add(new Avaliacao { ViagemId = _viagemId, AvaliadorId = _externo, AvaliadoId = _passageiroA, Classificacao = 4 });
        var resumo = (await servico.ConsultarAsync(_passageiroA, CancellationToken.None)).Valor;
        Assert.Equal(4.3m, resumo.Media);
        Assert.Equal(3, resumo.Total);
    }

    [Fact]
    public async Task Consultar_RefleteAlteracaoNaMedia()
    {
        var servico = CriarServico();
        await servico.CriarAsync(_viagemId, _condutor, _passageiroA, 3, CancellationToken.None);
        Assert.Equal(3m, (await servico.ConsultarAsync(_passageiroA, CancellationToken.None)).Valor.Media);
        await servico.AlterarAsync(_condutor, _passageiroA, 5, CancellationToken.None);
        Assert.Equal(5m, (await servico.ConsultarAsync(_passageiroA, CancellationToken.None)).Valor.Media);
    }

    [Fact]
    public async Task Consultar_NaoExpoeIdentidadeDoAvaliador()
    {
        var servico = CriarServico();
        var registo = (await servico.CriarAsync(_viagemId, _condutor, _passageiroA, 5, CancellationToken.None)).Valor;
        var resumo = (await servico.ConsultarAsync(_passageiroA, CancellationToken.None)).Valor;
        var jsonCriacao = System.Text.Json.JsonSerializer.Serialize(registo);
        var jsonConsulta = System.Text.Json.JsonSerializer.Serialize(resumo);
        Assert.DoesNotContain(_condutor.ToString(), jsonCriacao, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(_condutor.ToString(), jsonConsulta, StringComparison.OrdinalIgnoreCase);
        Assert.Single(resumo.Avaliacoes);
        Assert.Equal(5, resumo.Avaliacoes[0].Classificacao);
    }

    [Fact]
    public async Task Consultar_RejeitaUtilizadorInexistente()
    {
        var servico = CriarServico();
        Assert.Equal(TipoErro.NaoEncontrado, (await servico.ConsultarAsync(Guid.NewGuid(), CancellationToken.None)).Erro?.Tipo);
    }

    [Fact]
    public async Task Criar_GravacaoRecusadaPelaBd_Conflito()
    {
        var servico = CriarServico();
        _repo.RecusarGravacao = true;

        var resultado = await servico.CriarAsync(_viagemId, _condutor, _passageiroA, 5, CancellationToken.None);

        Assert.Equal(TipoErro.Conflito, resultado.Erro?.Tipo);
        Assert.Equal("Não foi possível registar a avaliação; poderá já existir.", resultado.Erro?.Mensagem);
    }

    [Fact]
    public async Task Alterar_GravacaoRecusadaPelaBd_Conflito()
    {
        var servico = CriarServico();
        await servico.CriarAsync(_viagemId, _condutor, _passageiroA, 3, CancellationToken.None);
        _repo.RecusarGravacao = true;

        var resultado = await servico.AlterarAsync(_condutor, _passageiroA, 5, CancellationToken.None);

        Assert.Equal(TipoErro.Conflito, resultado.Erro?.Tipo);
        Assert.Equal("Não foi possível alterar a avaliação.", resultado.Erro?.Mensagem);
    }

    private sealed class RepositorioEmMemoria : IRepositorioAvaliacoes
    {
        public Dictionary<Guid, Viagem> Viagens { get; } = new();
        public HashSet<Guid> Utilizadores { get; } = new();
        public HashSet<(Guid ViagemId, Guid PassageiroId)> Aceites { get; } = new();
        public List<Avaliacao> Avaliacoes { get; } = new();
        public bool RecusarGravacao { get; set; }

        public Task<Viagem?> ObterViagemAsync(Guid viagemId, CancellationToken ct) =>
            Task.FromResult(Viagens.GetValueOrDefault(viagemId));
        public Task<bool> UtilizadorExisteAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Utilizadores.Contains(id));
        public Task<bool> ParticipacaoAceiteAsync(Guid viagemId, Guid passageiroId, CancellationToken ct) =>
            Task.FromResult(Aceites.Contains((viagemId, passageiroId)));
        public Task<Avaliacao?> ObterPorParAsync(Guid avaliadorId, Guid avaliadoId, CancellationToken ct) =>
            Task.FromResult(Avaliacoes.FirstOrDefault(a => a.AvaliadorId == avaliadorId && a.AvaliadoId == avaliadoId));
        public Task<bool> GuardarAsync(Avaliacao avaliacao, CancellationToken ct)
        {
            if (RecusarGravacao) return Task.FromResult(false);
            if (!Avaliacoes.Contains(avaliacao)) Avaliacoes.Add(avaliacao);
            return Task.FromResult(true);
        }
        public Task<IReadOnlyList<Avaliacao>> ListarRecebidasAsync(Guid utilizadorId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Avaliacao>>(Avaliacoes.Where(a => a.AvaliadoId == utilizadorId).ToList());
        public Task<IReadOnlyList<ClassificacaoRecebida>> ListarClassificacoesAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ClassificacaoRecebida>>(Avaliacoes
                .Where(a => ids.Contains(a.AvaliadoId))
                .Select(a => new ClassificacaoRecebida(a.AvaliadoId, a.Classificacao))
                .ToList());
    }
}
