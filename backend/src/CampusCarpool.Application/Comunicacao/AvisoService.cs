using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Notificacoes;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Rules;

namespace CampusCarpool.Application.Comunicacao;

/// <summary>Implementação de <see cref="IAvisoService"/>.</summary>
/// <remarks>
/// Um aviso é guardado uma vez em <c>Avisos</c>. Para cada participação aceite é criada uma cópia dirigida em
/// <c>Notificacoes</c>. As duas gravações são um só <c>SaveChanges</c>, que é atómico, para não deixar avisos sem os
/// seus destinatários (AP-10). As notificações de aviso não geram push (RF-F006-17).
/// </remarks>
public sealed class AvisoService(
    IRepositorioViagens viagens,
    IRepositorioParticipacoes participacoes,
    IRepositorioAvisos avisos,
    INotificacaoService notificacoes,
    IUnidadeDeTrabalho unidadeDeTrabalho) : IAvisoService
{
    // P-28 (100 caracteres); o mesmo limite está no DbContext (HasMaxLength(100)).
    private const int TamanhoMaximoConteudo = 100;

    private static readonly Erro ViagemNaoEncontrada = new(TipoErro.NaoEncontrado, "Viagem não encontrada.");
    private static readonly Erro Proibido = new(TipoErro.Proibido, "Não tens permissão para aceder aos avisos desta viagem.");

    /// <inheritdoc />
    public async Task<Resultado<AvisoCriado>> CriarAsync(
        Guid viagemId, Guid condutorId, string? conteudo, CancellationToken ct)
    {
        var texto = conteudo?.Trim();
        if (string.IsNullOrWhiteSpace(texto))
            return new Erro(TipoErro.DadosInvalidos, "Escreve o texto do aviso.");

        if (texto.Length > TamanhoMaximoConteudo)
            return new Erro(TipoErro.DadosInvalidos, $"O aviso tem no máximo {TamanhoMaximoConteudo} caracteres.");

        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);
        if (viagem is null) return ViagemNaoEncontrada;
        if (viagem.CondutorId != condutorId) return Proibido;

        // RF-F005-05 (DEF-BE-017): só em viagens Ativas.
        if (!viagem.EstaAtiva)
            return new Erro(TipoErro.Conflito, "A viagem não está ativa.");

        var aviso = new Aviso
        {
            ViagemId = viagemId,
            Conteudo = texto
        };

        avisos.Adicionar(aviso);

        var aceites = await participacoes.ListarPorViagemAsync(viagemId, EstadoParticipacao.Aceite, ct);

        notificacoes.RegistarAviso(viagemId, aceites.Select(p => p.PassageiroId), texto);

        await unidadeDeTrabalho.ConfirmarAsync(ct);

        return new AvisoCriado(aviso.Id, aviso.Conteudo, aviso.CriadoEm);
    }

    /// <inheritdoc />
    public async Task<Resultado<List<AvisoDaViagem>>> ListarAsync(Guid viagemId, Guid utilizadorId, CancellationToken ct)
    {
        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);
        if (viagem is null) return ViagemNaoEncontrada;

        var aceite = await participacoes.ExisteAceiteAsync(viagemId, utilizadorId, ct);
        if (!PoliticaDeVisibilidade.ParticipaNaViagem(viagem, utilizadorId, aceite)) return Proibido;

        var lista = await avisos.ListarPorViagemAsync(viagemId, ct);

        // O autor de todos os avisos é o condutor da viagem (RF-F005-07).
        return lista.Select(a => new AvisoDaViagem(a.Id, a.ViagemId, viagem.CondutorId, a.Conteudo, a.CriadoEm)).ToList();
    }
}
