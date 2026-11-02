using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
namespace CampusCarpool.Application.Services;
public sealed class AvaliacaoService(IRepositorioAvaliacoes repositorio) : IAvaliacaoService
{
    private static void ValidarClassificacao(int classificacao)
    {
        if (classificacao is < 1 or > 5) throw new ArgumentException("A classificação deve ser um número inteiro entre 1 e 5.");
    }
    public async Task<AvaliacaoRegistada> CriarAsync(Guid viagemId, Guid avaliadorId, Guid avaliadoId, int classificacao, CancellationToken ct)
    {
        ValidarClassificacao(classificacao);
        if (avaliadorId == avaliadoId) throw new ArgumentException("Não é permitido avaliar-se a si próprio.");
        var viagem = await repositorio.ObterViagemAsync(viagemId, ct) ?? throw new KeyNotFoundException("Viagem não encontrada.");
        if (!await repositorio.UtilizadorExisteAsync(avaliadorId, ct) || !await repositorio.UtilizadorExisteAsync(avaliadoId, ct))
            throw new KeyNotFoundException("Utilizador não encontrado.");
        if (viagem.Estado != "Concluída" && viagem.Estado != "Concluida")
            throw new InvalidOperationException("Só é possível avaliar viagens concluídas.");
        var avaliadorElegivel = viagem.CondutorId == avaliadorId || await repositorio.ParticipacaoAceiteAsync(viagemId, avaliadorId, ct);
        var avaliadoElegivel = viagem.CondutorId == avaliadoId || await repositorio.ParticipacaoAceiteAsync(viagemId, avaliadoId, ct);
        if (!avaliadorElegivel || !avaliadoElegivel)
            throw new InvalidOperationException("Ambos os utilizadores devem ter participado na viagem concluída com participação aceite.");
        if (await repositorio.ObterPorParAsync(avaliadorId, avaliadoId, ct) is not null)
            throw new InvalidOperationException("Já existe uma avaliação deste utilizador. Pode alterar a avaliação existente.");
        var nova = await repositorio.GuardarAsync(new Avaliacao { ViagemId = viagemId, AvaliadorId = avaliadorId, AvaliadoId = avaliadoId, Classificacao = (byte)classificacao }, ct);
        return new AvaliacaoRegistada(nova.Classificacao, nova.CriadoEm);
    }
    public async Task<AvaliacaoRegistada> AlterarAsync(Guid avaliadorId, Guid avaliadoId, int classificacao, CancellationToken ct)
    {
        ValidarClassificacao(classificacao);
        var avaliacao = await repositorio.ObterPorParAsync(avaliadorId, avaliadoId, ct)
            ?? throw new KeyNotFoundException("Não existe uma avaliação sua para este utilizador.");
        avaliacao.Classificacao = (byte)classificacao;
        var atualizada = await repositorio.GuardarAsync(avaliacao, ct);
        return new AvaliacaoRegistada(atualizada.Classificacao, atualizada.CriadoEm);
    }
    public async Task<ResumoAvaliacoes> ConsultarAsync(Guid utilizadorId, CancellationToken ct)
    {
        if (!await repositorio.UtilizadorExisteAsync(utilizadorId, ct)) throw new KeyNotFoundException("Utilizador não encontrado.");
        var recebidas = await repositorio.ListarRecebidasAsync(utilizadorId, ct);
        decimal? media = recebidas.Count == 0 ? null : Math.Round(recebidas.Average(x => (decimal)x.Classificacao), 1, MidpointRounding.AwayFromZero);
        return new ResumoAvaliacoes(utilizadorId, recebidas.Count, media,
            recebidas.Select(x => new AvaliacaoPublica(x.Classificacao, x.CriadoEm)).ToList());
    }
}
