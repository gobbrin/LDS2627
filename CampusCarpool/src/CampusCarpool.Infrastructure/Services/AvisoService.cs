using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Services;

/// <summary>Implementa a criação de avisos e a sua distribuição persistida.</summary>
/// <remarks>
/// Um aviso é guardado uma vez em <c>Avisos</c>. Para cada participação aceite
/// é criada uma cópia dirigida em <c>Notificacoes</c>. As duas operações usam
/// a mesma transação para não deixar avisos sem os seus destinatários.
/// </remarks>
public sealed class AvisoService(CampusCarpoolDbContext db) : IAvisoService
{
    private const int TamanhoMaximoConteudo = 100;

    public async Task<(ResultadoCriacaoAviso Resultado, Aviso? Aviso)> CriarAsync(
        Guid viagemId, Guid condutorId, string? conteudo, CancellationToken ct)
    {
        var texto = conteudo?.Trim();
        if (string.IsNullOrWhiteSpace(texto) || texto.Length > TamanhoMaximoConteudo)
            return (ResultadoCriacaoAviso.ConteudoInvalido, null);

        var viagem = await db.Viagens.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == viagemId, ct);
        if (viagem is null)
            return (ResultadoCriacaoAviso.ViagemNaoEncontrada, null);
        if (viagem.CondutorId != condutorId)
            return (ResultadoCriacaoAviso.NaoAutorizado, null);

        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, ct);

        var aviso = new Aviso
        {
            ViagemId = viagemId,
            CondutorId = condutorId,
            Conteudo = texto
        };
        db.Avisos.Add(aviso);

        var passageiros = await db.Participacoes
            .Where(p => p.ViagemId == viagemId && p.Estado == EstadoParticipacao.Aceite)
            .Select(p => p.PassageiroId)
            .Distinct()
            .ToListAsync(ct);

        db.Notificacoes.AddRange(passageiros.Select(passageiroId => new Notificacao
        {
            UtilizadorId = passageiroId,
            ViagemId = viagemId,
            Tipo = TipoNotificacao.Aviso,
            Titulo = "Aviso do condutor",
            Conteudo = texto
        }));

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return (ResultadoCriacaoAviso.Sucesso, aviso);
    }

    public async Task<(ResultadoConsultaAvisos Resultado, IReadOnlyList<Aviso> Avisos)> ListarAsync(
        Guid viagemId, Guid utilizadorId, CancellationToken ct)
    {
        var viagem = await db.Viagens.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == viagemId, ct);
        if (viagem is null)
            return (ResultadoConsultaAvisos.ViagemNaoEncontrada, Array.Empty<Aviso>());

        var autorizado = viagem.CondutorId == utilizadorId || await db.Participacoes.AnyAsync(
            p => p.ViagemId == viagemId && p.PassageiroId == utilizadorId &&
                 p.Estado == EstadoParticipacao.Aceite, ct);
        if (!autorizado)
            return (ResultadoConsultaAvisos.NaoAutorizado, Array.Empty<Aviso>());

        var avisos = await db.Avisos.AsNoTracking()
            .Where(a => a.ViagemId == viagemId)
            .OrderByDescending(a => a.CriadoEm)
            .ToListAsync(ct);
        return (ResultadoConsultaAvisos.Sucesso, avisos);
    }
}