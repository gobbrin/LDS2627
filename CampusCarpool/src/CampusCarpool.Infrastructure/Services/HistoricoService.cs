
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Services;

/// <summary>Implementação do histórico pessoal (F009) sobre o EF Core. Ver <see cref="IHistoricoService"/>.</summary>
/// <remarks>
/// Está na Infrastructure e é registado no <c>Program.cs</c> (AP-04). São seis consultas só de leitura, sem
/// paginação (AP-10). As viagens só aparecem depois de partirem ou de deixarem de estar ativas: as ativas
/// futuras ainda não são "histórico". As conversões de enums para texto são feitas em memória, depois do
/// <c>ToListAsync</c>, para não depender da tradução do fornecedor de BD.
/// </remarks>
public sealed class HistoricoService(
    CampusCarpoolDbContext db) : IHistoricoService
{
    /// <inheritdoc />
    public async Task<HistoricoResultado> ConsultarAsync(
        Guid utilizadorId,
        CancellationToken ct)
    {
        var agora = DateTime.UtcNow;

        // Viagens em que o utilizador foi condutor (RF-F009-02), da partida mais recente para a mais antiga (RF-F009-06)
        var condutor = await db.Viagens
            .AsNoTracking()
            .Where(v =>
                v.CondutorId == utilizadorId &&
                (v.PartidaEm <= agora || v.Estado != nameof(EstadoViagem.Ativa)))
            .OrderByDescending(v => v.PartidaEm)
            .Select(v => new ViagemHistorico(
                v.Id,
                v.Origem,
                v.Destino,
                v.PartidaEm,
                v.Estado,
                v.LugaresOferecidos,
                null,
                null))
            .ToListAsync(ct);

        // Participacoes do utilizador em viagens

        var participacoes = await db.Participacoes
            .AsNoTracking()
            .Where(p =>
                p.PassageiroId == utilizadorId &&
                // RF-F009-08: a indisponibilização cancela as participações aceites (P-25),
                // mas a viagem continua no histórico do passageiro, com o estado atual.
                // As canceladas pelo próprio passageiro ficam de fora até à decisão D-029.
                (p.Estado == EstadoParticipacao.Aceite ||
                 (p.Estado == EstadoParticipacao.Cancelada &&
                  p.AceiteEm != null &&
                  p.Viagem.Estado == nameof(EstadoViagem.Indisponibilizada))) &&
                (p.Viagem.PartidaEm <= agora ||
                 p.Viagem.Estado != nameof(EstadoViagem.Ativa)))
                    .OrderByDescending(p => p.Viagem.PartidaEm)
                    .Select(p => new
                    {
                        p.Id,
                        p.ViagemId,
                        p.Viagem.Origem,
                        p.Viagem.Destino,
                        p.Viagem.PartidaEm,
                        EstadoViagem = p.Viagem.Estado,
                        p.Estado
                    })
                    .ToListAsync(ct);

        var passageiro = participacoes
            .Select(p => new ViagemHistorico(
                p.ViagemId,
                p.Origem,
                p.Destino,
                p.PartidaEm,
                p.EstadoViagem,
                null,
                p.Id,
                p.Estado.ToString()))
            .ToList();

        // Pedidos efetuados pelo utilizador (RF-F009-04), em todos os estados
        var pedidosEfetuadosDb = await db.Participacoes
            .AsNoTracking()
            .Where(p => p.PassageiroId == utilizadorId)
            .OrderByDescending(p => p.CriadoEm)
            .Select(p => new
            {
                p.Id,
                p.ViagemId,
                p.Viagem.Origem,
                p.Viagem.Destino,
                p.Viagem.PartidaEm,
                p.CriadoEm,
                p.Estado
            })
            .ToListAsync(ct);

        var pedidosEfetuados = pedidosEfetuadosDb
            .Select(p => new PedidoHistorico(
                p.Id,
                p.ViagemId,
                p.Origem,
                p.Destino,
                p.PartidaEm,
                p.CriadoEm,
                p.Estado.ToString()))
            .ToList();

        // Pedidos recebidos nas viagens do condutor
        var pedidosRecebidosDb = await db.Participacoes
            .AsNoTracking()
            .Where(p => p.Viagem.CondutorId == utilizadorId)
            .OrderByDescending(p => p.CriadoEm)
            .Select(p => new
            {
                p.Id,
                p.ViagemId,
                p.Viagem.Origem,
                p.Viagem.Destino,
                p.Viagem.PartidaEm,
                p.CriadoEm,
                p.Estado
            })
            .ToListAsync(ct);

        var pedidosRecebidos = pedidosRecebidosDb
            .Select(p => new PedidoHistorico(
                p.Id,
                p.ViagemId,
                p.Origem,
                p.Destino,
                p.PartidaEm,
                p.CriadoEm,
                p.Estado.ToString()))
            .ToList();

        // Notificacoes pessoais (RF-F009-05); repete a lista paginada de /notificacoes, aqui sem paginação
        var notificacoesDb = await db.Notificacoes
            .AsNoTracking()
            .Where(n => n.UtilizadorId == utilizadorId)
            .OrderByDescending(n => n.CriadaEm)
            .Select(n => new
            {
                n.Id,
                n.ViagemId,
                n.Tipo,
                n.Titulo,
                n.Conteudo,
                n.CriadaEm,
                n.LidaEm
            })
            .ToListAsync(ct);

        var notificacoes = notificacoesDb
            .Select(n => new NotificacaoHistorico(
                n.Id,
                n.ViagemId,
                n.Tipo.ToString(),
                n.Titulo,
                n.Conteudo,
                n.CriadaEm,
                n.LidaEm))
            .ToList();

        // Avisos visiveis ao condutor ou passageiro aceite
        var avisosDb = await db.Avisos
            .AsNoTracking()
            .Where(a =>
                db.Viagens.Any(v =>
                    v.Id == a.ViagemId &&
                    v.CondutorId == utilizadorId)
                ||
                db.Participacoes.Any(p =>
                    p.ViagemId == a.ViagemId &&
                    p.PassageiroId == utilizadorId &&
                    p.Estado == EstadoParticipacao.Aceite))
            .OrderByDescending(a => a.CriadoEm)
            .Select(a => new AvisoHistorico(
                a.Id,
                a.ViagemId,
                a.Conteudo,
                a.CriadoEm))
            .ToListAsync(ct);

        return new HistoricoResultado(
            condutor,
            passageiro,
            pedidosEfetuados,
            pedidosRecebidos,
            notificacoes,
            avisosDb
        );
    }
}
