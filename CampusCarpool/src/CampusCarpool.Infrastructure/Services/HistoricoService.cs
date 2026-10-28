
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Services;

public sealed class HistoricoService(
    CampusCarpoolDbContext db) : IHistoricoService
{
    public async Task<HistoricoResultado> ConsultarAsync(
        Guid utilizadorId,
        CancellationToken ct)
    {
        var agora = DateTime.UtcNow;

        // Viagens em que o utilizador foi condutor
        var condutor = await db.Viagens
            .AsNoTracking()
            .Where(v =>
                v.CondutorId == utilizadorId &&
                (v.PartidaEm <= agora || v.Estado != "Ativa"))
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
        p.Estado == EstadoParticipacao.Aceite &&
        (p.Viagem.PartidaEm <= agora ||
         p.Viagem.Estado != "Ativa"))
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

        // Pedidos efetuados pelo utilizador
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

        // Notificacoes pessoais
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
