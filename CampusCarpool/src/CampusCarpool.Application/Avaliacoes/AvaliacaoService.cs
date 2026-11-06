using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Rules;
namespace CampusCarpool.Application.Avaliacoes;

/// <summary>Implementação das regras das avaliações (F007). Ver <see cref="IAvaliacaoService"/>.</summary>
/// <remarks>
/// A ordem das validações define o erro devolvido quando há vários problemas: valor, auto-avaliação,
/// existência, estado da viagem, elegibilidade e, por fim, avaliação repetida.
/// </remarks>
public sealed class AvaliacaoService(IRepositorioAvaliacoes repositorio) : IAvaliacaoService
{
    // Valor inteiro de 1 a 5 (RF-F007-01).
    private static void ValidarClassificacao(int classificacao)
    {
        if (classificacao is < 1 or > 5) throw new ArgumentException("A classificação deve ser um número inteiro entre 1 e 5.");
    }

    /// <inheritdoc />
    public async Task<AvaliacaoRegistada> CriarAsync(Guid viagemId, Guid avaliadorId, Guid avaliadoId, int classificacao, CancellationToken ct)
    {
        ValidarClassificacao(classificacao);
        if (avaliadorId == avaliadoId) throw new ArgumentException("Não é permitido avaliar-se a si próprio.");
        var viagem = await repositorio.ObterViagemAsync(viagemId, ct) ?? throw new KeyNotFoundException("Viagem não encontrada.");
        if (!await repositorio.UtilizadorExisteAsync(avaliadorId, ct) || !await repositorio.UtilizadorExisteAsync(avaliadoId, ct))
            throw new KeyNotFoundException("Utilizador não encontrado.");
        if (viagem.Estado != nameof(EstadoViagem.Concluida))
            throw new InvalidOperationException("Só é possível avaliar viagens concluídas.");
        // P-16: o condutor conta como participante; o passageiro precisa de participação aceite.
        var avaliadorElegivel = viagem.CondutorId == avaliadorId || await repositorio.ParticipacaoAceiteAsync(viagemId, avaliadorId, ct);
        var avaliadoElegivel = viagem.CondutorId == avaliadoId || await repositorio.ParticipacaoAceiteAsync(viagemId, avaliadoId, ct);
        if (!avaliadorElegivel || !avaliadoElegivel)
            throw new InvalidOperationException("Ambos os utilizadores devem ter participado na viagem concluída com participação aceite.");
        // RF-F007-04: uma só avaliação por par, seja qual for a viagem. Dois pedidos simultâneos podem passar
        // esta verificação; numa BD migrada o índice único do par recusa o segundo, mas numa BD criada com
        // EnsureCreated (testes) o índice é o triplo e não recusa (IN-01).
        if (await repositorio.ObterPorParAsync(avaliadorId, avaliadoId, ct) is not null)
            throw new InvalidOperationException("Já existe uma avaliação deste utilizador. Pode alterar a avaliação existente.");
        var nova = await repositorio.GuardarAsync(new Avaliacao { ViagemId = viagemId, AvaliadorId = avaliadorId, AvaliadoId = avaliadoId, Classificacao = (byte)classificacao }, ct);
        return new AvaliacaoRegistada(nova.Classificacao, nova.CriadoEm);
    }

    /// <inheritdoc />
    public async Task<AvaliacaoRegistada> AlterarAsync(Guid avaliadorId, Guid avaliadoId, int classificacao, CancellationToken ct)
    {
        ValidarClassificacao(classificacao);
        var avaliacao = await repositorio.ObterPorParAsync(avaliadorId, avaliadoId, ct)
            ?? throw new KeyNotFoundException("Não existe uma avaliação sua para este utilizador.");
        // Só o valor muda; CriadoEm mantém a data original (DO-10, P-22 por decidir).
        avaliacao.Classificacao = (byte)classificacao;
        var atualizada = await repositorio.GuardarAsync(avaliacao, ct);
        return new AvaliacaoRegistada(atualizada.Classificacao, atualizada.CriadoEm);
    }

    /// <inheritdoc />
    public async Task<ResumoAvaliacoes> ConsultarAsync(Guid utilizadorId, CancellationToken ct)
    {
        if (!await repositorio.UtilizadorExisteAsync(utilizadorId, ct)) throw new KeyNotFoundException("Utilizador não encontrado.");
        var recebidas = await repositorio.ListarRecebidasAsync(utilizadorId, ct);
        // RF-F007-08: média aritmética, arredondada a 1 casa (o arredondamento não está nos requisitos);
        // calculada em cada consulta, por isso reflete logo qualquer criação ou alteração (RF-F007-09).
        var media = CalculadoraDeClassificacao.Media(recebidas.Select(x => x.Classificacao));
        return new ResumoAvaliacoes(utilizadorId, recebidas.Count, media,
            recebidas.Select(x => new AvaliacaoPublica(x.Classificacao, x.CriadoEm)).ToList());
    }
}
