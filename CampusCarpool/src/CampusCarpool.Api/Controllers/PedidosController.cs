using System.Security.Claims;
using CampusCarpool.Api.Services;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Services;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Api.Controllers;

/// <summary>Submissão de pedidos de participação (F004). Rota <c>/api/v1/pedidos</c>.</summary>
/// <remarks>
/// Só cria o pedido; aceitar, rejeitar e cancelar estão no <c>GestaoParticipacoesController</c>. Usa o
/// <see cref="CampusCarpoolDbContext"/> diretamente. Falta a lista dos pedidos pendentes de uma viagem para o
/// condutor (DEF-BE-016).
/// </remarks>
[ApiController]
[Authorize]
[Route("api/v1/pedidos")]
public sealed class PedidosController(
    CampusCarpoolDbContext db,
    MapasService mapas,
    INotificacaoService notificacaoService,
    INotificadorPedidosTempoReal notificadorPedidosTempoReal) : ControllerBase
{
    // Índice único filtrado (pendente ou aceite) criado no DbContext (I038); usado para reconhecer a violação.
    private const string IndicePedidosAtivos = "IX_Participacoes_ViagemId_PassageiroId";

    private const string MensagemPontosIncompativeis =
        "A recolha e o destino não podem ser o mesmo local, a recolha não pode ser o destino da viagem e o destino não pode ser a origem da viagem.";

    // RF-F004-18: recolha e destino efetivos; RF-F004-20: recolha no destino da viagem; RF-F004-21: destino na origem.
    private static bool PontosIncompativeis<T>(
        T recolha,
        T destino,
        T origemViagem,
        T destinoViagem,
        Func<T, T, bool> mesmoLocal) =>
        mesmoLocal(recolha, destino)
        || mesmoLocal(recolha, destinoViagem)
        || mesmoLocal(destino, origemViagem);

    /// <summary>Corpo de <c>POST /pedidos</c>. Pontos omitidos (<c>null</c>) significam a origem/o destino da viagem.</summary>
    public sealed record CriarPedido(
        Guid ViagemId,
        string? Recolha,
        string? Destino);

    // Id do utilizador autenticado (claim "sub").
    private Guid? UtilizadorId =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub"),
            out var id)
            ? id
            : null;

    /// <summary>Submete um pedido de participação, no estado <c>Pendente</c> (RF-F004-01 a RF-F004-07, RF-F004-13).</summary>
    /// <remarks>
    /// Não reserva lugar (só a aceitação o faz). Os pontos indicados são geocodificados e as coordenadas ficam na
    /// participação, para a rota não voltar a pedi-las (A8, DC-34). Depois de gravar, cria a notificação "Pedido
    /// recebido" para o condutor (com push, RF-F006-13) e envia o evento em tempo real (RF-F006-15).
    /// </remarks>
    /// <returns>
    /// 201 pedido criado; 400 sem nenhum ponto (P-20), pontos vazios ou com mais de 250 caracteres, ou pontos no
    /// mesmo local (RF-F004-18, RF-F004-20, RF-F004-21); 401; 403 conta por confirmar; 404 viagem
    /// inexistente; 409 viagem não ativa ou já partida, própria viagem (RF-F004-05), pedido repetido (RF-F004-06) ou
    /// sem lugares; 422 ponto não encontrado; 502/503/504 problema no serviço de mapas.
    /// </returns>
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

        if (viagem.Estado != nameof(EstadoViagem.Ativa) || viagem.PartidaEm <= DateTime.UtcNow)
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

        // P-20: pelo menos um dos dois pontos; os valores fornecidos devem ser válidos.
        if (input.Recolha is null && input.Destino is null)
        {
            return BadRequest(new
            {
                erro = "Indica um ponto de recolha ou de destino."
            });
        }

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

        // Primeiro só pelo texto, para não chamar o serviço de mapas sem necessidade.
        if (PontosIncompativeis(
            input.Recolha ?? viagem.Origem,
            input.Destino ?? viagem.Destino,
            viagem.Origem,
            viagem.Destino,
            RegraMesmoLocal.MesmoTexto))
        {
            return BadRequest(new { erro = MensagemPontosIncompativeis });
        }

        // Verificar se os locais fornecidos podem ser identificados pelo serviço de mapas.
        Coordenadas? coordenadasRecolha = null;
        Coordenadas? coordenadasDestino = null;

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
                coordenadasRecolha = await mapas.GeocodificarAsync(
                    input.Recolha.Trim(), ct);

                if (coordenadasRecolha is null)
                {
                    return UnprocessableEntity(new
                    {
                        erro = "Ponto de recolha não encontrado."
                    });
                }
            }

            if (input.Destino is not null)
            {
                coordenadasDestino = await mapas.GeocodificarAsync(
                    input.Destino.Trim(), ct);

                if (coordenadasDestino is null)
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

        // As mesmas regras, agora pelas coordenadas. Um ponto omitido usa as coordenadas da viagem (P-20).
        var origemViagem = new Coordenadas(viagem.OrigemLatitude, viagem.OrigemLongitude);
        var destinoViagem = new Coordenadas(viagem.DestinoLatitude, viagem.DestinoLongitude);

        if (PontosIncompativeis(
            coordenadasRecolha ?? origemViagem,
            coordenadasDestino ?? destinoViagem,
            origemViagem,
            destinoViagem,
            (a, b) => RegraMesmoLocal.MesmasCoordenadas(a.Latitude, a.Longitude, b.Latitude, b.Longitude)))
        {
            return BadRequest(new { erro = MensagemPontosIncompativeis });
        }

        // Criar a participação pendente sem descontar lugares.
        var pedido = new Participacao
        {
            ViagemId = viagem.Id,
            PassageiroId = passageiroId,
            Estado = EstadoParticipacao.Pendente,
            Recolha = input.Recolha?.Trim(),
            Destino = input.Destino?.Trim(),
            RecolhaLatitude = coordenadasRecolha?.Latitude,
            RecolhaLongitude = coordenadasRecolha?.Longitude,
            DestinoLatitude = coordenadasDestino?.Latitude,
            DestinoLongitude = coordenadasDestino?.Longitude,
            CriadoEm = DateTime.UtcNow
        };

        db.Participacoes.Add(pedido);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        // Dois pedidos simultâneos podem passar a verificação acima; o índice único recusa o segundo
        // (erros 2601/2627 do SQL Server). Em SQLite (testes) a exceção é outra e não é apanhada aqui.
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
