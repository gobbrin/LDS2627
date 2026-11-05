
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Services;

/// <summary>Implementação do histórico pessoal (F009) sobre o EF Core. Ver <see cref="IHistoricoService"/>.</summary>
/// <remarks>
/// Está na Infrastructure e é registado no <c>Program.cs</c> (AP-04). São seis consultas só de leitura, sem
/// paginação (AP-10). As viagens aparecem em qualquer estado, incluindo as ativas futuras (RF-F009-02, RF-F009-03,
/// D-029): o separador Viagens junta as do condutor e aquelas em que o passageiro chegou a ser aceite; os pedidos
/// nunca aceites ficam só no separador Pedidos. As conversões de enums para texto são feitas em memória, depois
/// do <c>ToListAsync</c>, para não depender da tradução do fornecedor de BD.
/// </remarks>
public sealed class HistoricoService(
    CampusCarpoolDbContext db) : IHistoricoService
{
    /// <inheritdoc />
    public async Task<HistoricoResultado> ConsultarAsync(
        Guid utilizadorId,
        CancellationToken ct)
    {
        // Viagens em que o utilizador foi condutor (RF-F009-02), da partida mais recente para a mais antiga (RF-F009-06)
        var condutor = await db.Viagens
            .AsNoTracking()
            .Where(v => v.CondutorId == utilizadorId)
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
            // RF-F009-03 / D-029: conta quem chegou a ser aceite, mesmo que a participação tenha sido cancelada
            // depois, pelo passageiro ou pela indisponibilização (P-25).
            .Where(p => p.PassageiroId == utilizadorId && p.AceiteEm != null)
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
