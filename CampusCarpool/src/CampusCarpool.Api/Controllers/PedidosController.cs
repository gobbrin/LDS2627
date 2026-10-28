using System.Security.Claims;
using CampusCarpool.Api.Services;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/pedidos")]
public sealed class PedidosController(
    CampusCarpoolDbContext db,
    MapasService mapas,
    INotificacaoService notificacaoService,
    INotificadorPedidosTempoReal notificadorPedidosTempoReal) : ControllerBase
{
    private const string IndicePedidosAtivos = "IX_Participacoes_ViagemId_PassageiroId";

    public sealed record CriarPedido(
        Guid ViagemId,
        string? Recolha,
        string? Destino);

    private Guid? UtilizadorId =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub"),
            out var id)
            ? id
            : null;

    [HttpPost]
    public async Task<IActionResult> Criar(CriarPedido input, CancellationToken ct)
    {
        if (UtilizadorId is not Guid passageiroId)
            return Unauthorized();

        // Confirmar que o passageiro tem uma conta validada.
        var contaConfirmada = await db.Utilizadores.AnyAsync(
            u => u.Id == passageiroId && u.EmailConfirmado, ct);

        if (!contaConfirmada)
            return Forbid();

        // A viagem deve existir, estar ativa e ainda não ter começado.
        var viagem = await db.Viagens.FirstOrDefaultAsync(
            v => v.Id == input.ViagemId, ct);

        if (viagem is null)
            return NotFound(new { erro = "Viagem não encontrada." });

        if (viagem.Estado != "Ativa" || viagem.PartidaEm <= DateTime.UtcNow)
            return Conflict(new { erro = "Viagem indisponível." });

        if (viagem.CondutorId == passageiroId)
        {
            return Conflict(new
            {
                erro = "Não podes pedir participação na tua própria viagem."
            });
        }

        // Impedir pedidos pendentes ou participações aceites duplicadas.
        var pedidoExistente = await db.Participacoes.AnyAsync(
            p => p.ViagemId == viagem.Id
                 && p.PassageiroId == passageiroId
                 && (p.Estado == EstadoParticipacao.Pendente || p.Estado == EstadoParticipacao.Aceite),
            ct);

        if (pedidoExistente)
        {
            return Conflict(new
            {
                erro = "Já existe um pedido ou participação ativa nesta viagem."
            });
        }

        // Apenas as participações aceites ocupam lugares.
        var lugaresOcupados = await db.Participacoes.CountAsync(
            p => p.ViagemId == viagem.Id && p.Estado == EstadoParticipacao.Aceite, ct);

        if (lugaresOcupados >= viagem.LugaresOferecidos)
        {
            return Conflict(new
            {
                erro = "Não existem lugares disponíveis nesta viagem."
            });
        }

        // Os pontos são opcionais, mas os valores fornecidos devem ser válidos.
        if ((input.Recolha is not null &&
             (string.IsNullOrWhiteSpace(input.Recolha) || input.Recolha.Length > 250)) ||
            (input.Destino is not null &&
             (string.IsNullOrWhiteSpace(input.Destino) || input.Destino.Length > 250)))
        {
            return BadRequest(new
            {
                erro = "Os pontos de recolha ou destino são inválidos."
            });
        }

        // Verificar se os locais fornecidos podem ser identificados pelo serviço de mapas.
        if (input.Recolha is not null || input.Destino is not null)
        {
            if (!mapas.Configurado)
            {
                return StatusCode(503, new
                {
                    erro = "Serviço de mapas não configurado."
                });
            }

            try
            {
                if (input.Recolha is not null)
                {
                    var recolha = await mapas.GeocodificarAsync(
                        input.Recolha.Trim(), ct);

                    if (recolha is null)
                    {
                        return UnprocessableEntity(new
                        {
                            erro = "Ponto de recolha não encontrado."
                        });
                    }
                }

                if (input.Destino is not null)
                {
                    var destino = await mapas.GeocodificarAsync(
                        input.Destino.Trim(), ct);

                    if (destino is null)
                    {
                        return UnprocessableEntity(new
                        {
                            erro = "Ponto de destino não encontrado."
                        });
                    }
                }
            }
            catch (HttpRequestException)
            {
                return StatusCode(502, new
                {
                    erro = "Serviço de mapas indisponível."
                });
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                return StatusCode(504, new
                {
                    erro = "Tempo de resposta dos mapas excedido."
                });
            }
        }

        // Criar a participação pendente sem descontar lugares.
        var pedido = new Participacao
        {
            ViagemId = viagem.Id,
            PassageiroId = passageiroId,
            Estado = EstadoParticipacao.Pendente,
            Recolha = input.Recolha?.Trim(),
            Destino = input.Destino?.Trim(),
            CriadoEm = DateTime.UtcNow
        };

        db.Participacoes.Add(pedido);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException sqlEx
            && (sqlEx.Number == 2601 || sqlEx.Number == 2627)
            && sqlEx.Message.Contains(IndicePedidosAtivos, StringComparison.Ordinal))
        {
            return Conflict(new
            {
                erro = "Já existe um pedido ou participação ativa nesta viagem."
            });
        }

        //Notifica o condutor após o registo bem-sucedido do pedido.
        await notificacaoService.CriarAsync(
            viagem.CondutorId, 
            TipoNotificacao.PedidoRecebido, 
            "Recebeste um novo pedido de participação numa viagem.",
            ct);

        await notificadorPedidosTempoReal.PedidoRecebidoAsync(
            viagem.CondutorId,
            pedido.Id,
            ct);


        return StatusCode(StatusCodes.Status201Created, new
        {
            pedido.Id,
            pedido.ViagemId,
            pedido.PassageiroId,
            pedido.Estado,
            pedido.Recolha,
            pedido.Destino,
            pedido.CriadoEm
        });
    }
}
