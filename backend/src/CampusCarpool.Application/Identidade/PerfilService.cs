using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Avaliacoes;
using CampusCarpool.Application.Comum;
using CampusCarpool.Domain.Entities;

namespace CampusCarpool.Application.Identidade;

/// <summary>Implementação de <see cref="IPerfilService"/>.</summary>
/// <remarks>
/// Só altera o perfil do utilizador do token, por isso ninguém altera o perfil de outro (RF-F001-18).
/// </remarks>
public sealed class PerfilService(
    IRepositorioUtilizadores utilizadores,
    IRepositorioLigacoesExternas ligacoes,
    IAvaliacaoService avaliacoes,
    IUnidadeDeTrabalho unidadeDeTrabalho) : IPerfilService
{
    // P-27: número máximo de ligações externas por utilizador.
    private const int MaximoLigacoes = 5;
    private const int TamanhoMaximoUrl = 2048;
    private const int TamanhoMaximoDescricao = 100;

    private static readonly Erro UtilizadorNaoEncontrado = new(TipoErro.NaoEncontrado, "Utilizador não encontrado.");

    /// <inheritdoc />
    public async Task<Resultado<PerfilProprio>> ConsultarAsync(Guid utilizadorId, CancellationToken ct)
    {
        var utilizador = await utilizadores.ObterPorIdAsync(utilizadorId, ct);
        if (utilizador is null) return UtilizadorNaoEncontrado;

        // RF-F007-10: o próprio perfil também apresenta a classificação.
        var resumo = await avaliacoes.ConsultarAsync(utilizadorId, ct);
        if (resumo.Erro is { } erro) return erro;

        return new PerfilProprio(
            utilizador.Id,
            utilizador.Nome,
            utilizador.Email,
            utilizador.EmailConfirmado,
            utilizador.Curso,
            utilizador.Instituicao,
            utilizador.CriadoEm,
            await ListarLigacoesAsync(utilizadorId, ct),
            resumo.Valor.Media,
            resumo.Valor.Total);
    }

    /// <inheritdoc />
    public async Task<Resultado<PerfilPublico>> ConsultarPublicoAsync(Guid utilizadorId, CancellationToken ct)
    {
        var utilizador = await utilizadores.ObterPorIdAsync(utilizadorId, ct);
        if (utilizador is null) return UtilizadorNaoEncontrado;

        var lista = await ListarLigacoesAsync(utilizadorId, ct);
        var resumo = await avaliacoes.ConsultarAsync(utilizadorId, ct);
        if (resumo.Erro is { } erro) return erro;

        return new PerfilPublico(
            utilizador.Id,
            utilizador.Nome,
            utilizador.Curso,
            utilizador.Instituicao,
            lista,
            resumo.Valor.Media,
            resumo.Valor.Total);
    }

    /// <inheritdoc />
    public async Task<Resultado<PerfilAtualizado>> AtualizarAsync(
        Guid utilizadorId,
        string nome,
        string? curso,
        string? instituicao,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(nome)
            || nome.Trim().Length > 100
            || curso?.Trim().Length > 150
            || instituicao?.Trim().Length > 150)
        {
            return new Erro(TipoErro.DadosInvalidos, "Dados de perfil inválidos.");
        }

        var utilizador = await utilizadores.ObterParaAlterarAsync(utilizadorId, ct);
        if (utilizador is null) return UtilizadorNaoEncontrado;

        utilizador.AtualizarPerfil(
            nome.Trim(),
            string.IsNullOrWhiteSpace(curso) ? null : curso.Trim(),
            string.IsNullOrWhiteSpace(instituicao) ? null : instituicao.Trim());

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return new PerfilAtualizado(
            utilizador.Id,
            utilizador.Nome,
            utilizador.Email,
            utilizador.EmailConfirmado,
            utilizador.Curso,
            utilizador.Instituicao);
    }

    /// <inheritdoc />
    public async Task<Resultado<LigacaoPerfil>> AdicionarLigacaoAsync(
        Guid utilizadorId,
        string? url,
        string? descricao,
        CancellationToken ct)
    {
        // RF-F001-16: recusa, por exemplo, javascript:alert(1) ou texto sem esquema.
        var urlValido = url?.Length <= TamanhoMaximoUrl
            && Uri.TryCreate(url.Trim(), UriKind.Absolute, out var endereco)
            && (endereco.Scheme == Uri.UriSchemeHttp || endereco.Scheme == Uri.UriSchemeHttps);

        if (!urlValido || descricao?.Trim().Length > TamanhoMaximoDescricao)
            return new Erro(TipoErro.DadosInvalidos, "Ligação inválida: indica um URL http ou https.");

        var existentes = await ligacoes.ListarAsync(utilizadorId, ct);

        if (existentes.Count >= MaximoLigacoes)
            return new Erro(TipoErro.DadosInvalidos, "O perfil já tem o número máximo de ligações.");

        var ligacao = new LigacaoExterna
        {
            UtilizadorId = utilizadorId,
            Url = url!.Trim(),
            Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim(),
            Ordem = existentes.Count == 0 ? 1 : existentes.Max(l => l.Ordem) + 1
        };

        ligacoes.Adicionar(ligacao);
        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return new LigacaoPerfil(ligacao.Id, ligacao.Url, ligacao.Descricao);
    }

    /// <inheritdoc />
    public async Task<Resultado> RemoverLigacaoAsync(Guid utilizadorId, Guid ligacaoId, CancellationToken ct)
    {
        var ligacao = await ligacoes.ObterAsync(ligacaoId, utilizadorId, ct);

        if (ligacao is null)
            return new Erro(TipoErro.NaoEncontrado, "Ligação não encontrada.");

        ligacoes.Remover(ligacao);
        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return Resultado.Sucesso;
    }

    private async Task<List<LigacaoPerfil>> ListarLigacoesAsync(Guid utilizadorId, CancellationToken ct)
    {
        var lista = await ligacoes.ListarAsync(utilizadorId, ct);

        return lista.Select(l => new LigacaoPerfil(l.Id, l.Url, l.Descricao)).ToList();
    }
}
