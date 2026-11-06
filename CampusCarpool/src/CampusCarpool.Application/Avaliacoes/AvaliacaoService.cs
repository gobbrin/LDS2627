using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Rules;

namespace CampusCarpool.Application.Avaliacoes;

/// <summary>Implementação das regras das avaliações (F007). Ver <see cref="IAvaliacaoService"/>.</summary>
/// <remarks>
/// A ordem das validações define o erro devolvido quando há vários problemas: valor, auto-avaliação,
/// existência, estado da viagem, elegibilidade e, por fim, avaliação repetida.
/// </remarks>
public sealed class AvaliacaoService(IRepositorioAvaliacoes repositorio) : IAvaliacaoService
{
    private static readonly Erro ClassificacaoInvalida = new(TipoErro.DadosInvalidos, "A classificação deve ser um número inteiro entre 1 e 5.");
    private static readonly Erro UtilizadorNaoEncontrado = new(TipoErro.NaoEncontrado, "Utilizador não encontrado.");

    /// <inheritdoc />
    public async Task<Resultado<AvaliacaoRegistada>> CriarAsync(Guid viagemId, Guid avaliadorId, Guid avaliadoId, int classificacao, CancellationToken ct)
    {
        if (!Avaliacao.ValorValido(classificacao)) return ClassificacaoInvalida;

        if (avaliadorId == avaliadoId)
            return new Erro(TipoErro.DadosInvalidos, "Não é permitido avaliar-se a si próprio.");

        var viagem = await repositorio.ObterViagemAsync(viagemId, ct);

        if (viagem is null)
            return new Erro(TipoErro.NaoEncontrado, "Viagem não encontrada.");

        if (!await repositorio.UtilizadorExisteAsync(avaliadorId, ct) || !await repositorio.UtilizadorExisteAsync(avaliadoId, ct))
            return UtilizadorNaoEncontrado;

        if (!viagem.EstaConcluida)
            return new Erro(TipoErro.Conflito, "Só é possível avaliar viagens concluídas.");

        // P-16: o condutor conta como participante; o passageiro precisa de participação aceite.
        var avaliadorElegivel = viagem.CondutorId == avaliadorId || await repositorio.ParticipacaoAceiteAsync(viagemId, avaliadorId, ct);
        var avaliadoElegivel = viagem.CondutorId == avaliadoId || await repositorio.ParticipacaoAceiteAsync(viagemId, avaliadoId, ct);

        if (!avaliadorElegivel || !avaliadoElegivel)
            return new Erro(TipoErro.Conflito, "Ambos os utilizadores devem ter participado na viagem concluída com participação aceite.");

        // RF-F007-04: uma só avaliação por par, seja qual for a viagem. Dois pedidos simultâneos podem passar
        // esta verificação; numa BD migrada o índice único do par recusa o segundo, mas numa BD criada com
        // EnsureCreated (testes) o índice é o triplo e não recusa (IN-01).
        if (await repositorio.ObterPorParAsync(avaliadorId, avaliadoId, ct) is not null)
            return new Erro(TipoErro.Conflito, "Já existe uma avaliação deste utilizador. Pode alterar a avaliação existente.");

        var nova = Avaliacao.Criar(viagemId, avaliadorId, avaliadoId, classificacao);

        if (!await repositorio.GuardarAsync(nova, ct))
            return new Erro(TipoErro.Conflito, "Não foi possível registar a avaliação; poderá já existir.");

        return new AvaliacaoRegistada(nova.Classificacao, nova.CriadoEm);
    }

    /// <inheritdoc />
    public async Task<Resultado<AvaliacaoRegistada>> AlterarAsync(Guid avaliadorId, Guid avaliadoId, int classificacao, CancellationToken ct)
    {
        if (!Avaliacao.ValorValido(classificacao)) return ClassificacaoInvalida;

        var avaliacao = await repositorio.ObterPorParAsync(avaliadorId, avaliadoId, ct);

        if (avaliacao is null)
            return new Erro(TipoErro.NaoEncontrado, "Não existe uma avaliação sua para este utilizador.");

        avaliacao.Alterar(classificacao);

        if (!await repositorio.GuardarAsync(avaliacao, ct))
            return new Erro(TipoErro.Conflito, "Não foi possível alterar a avaliação.");

        return new AvaliacaoRegistada(avaliacao.Classificacao, avaliacao.CriadoEm);
    }

    /// <inheritdoc />
    public async Task<Resultado<ResumoAvaliacoes>> ConsultarAsync(Guid utilizadorId, CancellationToken ct)
    {
        if (!await repositorio.UtilizadorExisteAsync(utilizadorId, ct)) return UtilizadorNaoEncontrado;

        var recebidas = await repositorio.ListarRecebidasAsync(utilizadorId, ct);

        // RF-F007-08: média aritmética, arredondada a 1 casa (o arredondamento não está nos requisitos);
        // calculada em cada consulta, por isso reflete logo qualquer criação ou alteração (RF-F007-09).
        var media = CalculadoraDeClassificacao.Media(recebidas.Select(x => x.Classificacao));

        return new ResumoAvaliacoes(utilizadorId, recebidas.Count, media,
            recebidas.Select(x => new AvaliacaoPublica(x.Classificacao, x.CriadoEm)).ToList());
    }
}
