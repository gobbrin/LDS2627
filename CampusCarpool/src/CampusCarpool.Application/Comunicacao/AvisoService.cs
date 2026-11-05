using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;

namespace CampusCarpool.Application.Comunicacao;

/// <summary>Implementa a criação de avisos e a sua distribuição persistida.</summary>
/// <remarks>
/// Um aviso é guardado uma vez em <c>Avisos</c>. Para cada participação aceite
/// é criada uma cópia dirigida em <c>Notificacoes</c>. As duas operações usam
/// a mesma transação para não deixar avisos sem os seus destinatários.
/// As notificações de aviso não passam pelo <c>NotificacaoService</c>, por isso não geram push (RF-F006-17).
/// </remarks>
public sealed class AvisoService(
    IRepositorioViagens viagens,
    IRepositorioParticipacoes participacoes,
    IRepositorioAvisos avisos,
    IRepositorioNotificacoes notificacoes,
    IUnidadeDeTrabalho unidadeDeTrabalho) : IAvisoService
{
    // P-28 (100 caracteres); o mesmo limite está no DbContext (HasMaxLength(100)).
    private const int TamanhoMaximoConteudo = 100;

    /// <inheritdoc />
    public async Task<(ResultadoCriacaoAviso Resultado, Aviso? Aviso)> CriarAsync(
        Guid viagemId, Guid condutorId, string? conteudo, CancellationToken ct)
    {
        var texto = conteudo?.Trim();
        if (string.IsNullOrWhiteSpace(texto) || texto.Length > TamanhoMaximoConteudo)
            return (ResultadoCriacaoAviso.ConteudoInvalido, null);

        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);
        if (viagem is null)
            return (ResultadoCriacaoAviso.ViagemNaoEncontrada, null);
        if (viagem.CondutorId != condutorId)
            return (ResultadoCriacaoAviso.NaoAutorizado, null);

        // DEF-BE-017 (fica para m4): RF-F005-05 só permite avisos em viagens Ativas; o estado não é verificado.

        var aviso = new Aviso
        {
            ViagemId = viagemId,
            CondutorId = condutorId,
            Conteudo = texto
        };

        // Serializable: a lista de passageiros é lida dentro da transação que grava o aviso e as notificações.
        await unidadeDeTrabalho.EmTransacaoSerializavelAsync(async cancelamento =>
        {
            avisos.Adicionar(aviso);

            var aceites = await participacoes.ListarPorViagemAsync(viagemId, EstadoParticipacao.Aceite, cancelamento);

            notificacoes.AdicionarSemGravar(aceites.Select(p => p.PassageiroId).Distinct().Select(passageiroId => new Notificacao
            {
                UtilizadorId = passageiroId,
                ViagemId = viagemId,
                Tipo = TipoNotificacao.Aviso,
                Titulo = "Aviso do condutor",
                Conteudo = texto
            }));

            await unidadeDeTrabalho.ConfirmarAsync(cancelamento);
            return aviso;
        }, ct);

        return (ResultadoCriacaoAviso.Sucesso, aviso);
    }

    /// <inheritdoc />
    /// <remarks>Os avisos vêm do mais recente para o mais antigo.</remarks>
    public async Task<(ResultadoConsultaAvisos Resultado, IReadOnlyList<Aviso> Avisos)> ListarAsync(
        Guid viagemId, Guid utilizadorId, CancellationToken ct)
    {
        var viagem = await viagens.ObterPorIdAsync(viagemId, ct);
        if (viagem is null)
            return (ResultadoConsultaAvisos.ViagemNaoEncontrada, Array.Empty<Aviso>());

        var autorizado = viagem.CondutorId == utilizadorId
            || await participacoes.ExisteAceiteAsync(viagemId, utilizadorId, ct);
        if (!autorizado)
            return (ResultadoConsultaAvisos.NaoAutorizado, Array.Empty<Aviso>());

        return (ResultadoConsultaAvisos.Sucesso, await avisos.ListarPorViagemAsync(viagemId, ct));
    }
}
