using System.Security.Claims;
using CampusCarpool.Api.Services;
using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Infrastructure.Persistence;
using CampusCarpool.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Api.Controllers;

[ApiController, Authorize, Route("api/v1/viagens")]
public sealed class ViagensController(
    CampusCarpoolDbContext db,
    MapasService maps,
    EstimativaCustoService? custos = null,
    ConsultaRotaViagemService? consultaRotas = null,
    INotificacaoService? notificacaoService = null) : ControllerBase
{
    public sealed record CriarViagem(string Origem, string Destino, DateTime PartidaEm, int Lugares);
    public sealed record EditarViagem(DateTime PartidaEm, int Lugares);

    private Guid? Id => Guid.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
        out var id) ? id : null;

    [HttpPost]
    public async Task<IActionResult> Criar(CriarViagem input, CancellationToken ct)
    {
        if (Id is not Guid id) return Unauthorized();
        if (!await db.Utilizadores.AnyAsync(u => u.Id == id && u.EmailConfirmado, ct))
            return Forbid();
        if (string.IsNullOrWhiteSpace(input.Origem) || string.IsNullOrWhiteSpace(input.Destino) ||
            input.Origem.Length > 250 || input.Destino.Length > 250 ||
            string.Equals(input.Origem.Trim(), input.Destino.Trim(), StringComparison.OrdinalIgnoreCase) ||
            input.PartidaEm.Kind != DateTimeKind.Utc || input.PartidaEm <= DateTime.UtcNow ||
            input.Lugares is < 1 or > 8)
            return BadRequest(new { erro = "Dados de viagem inválidos." });

        if (!maps.Configurado)
            return StatusCode(503, new { erro = "Serviço de mapas não configurado." });

        ResultadoRota? rota;
        try
        {
            var origem = await maps.GeocodificarAsync(input.Origem, ct);
            var destino = await maps.GeocodificarAsync(input.Destino, ct);
            if (origem is null || destino is null)
                return UnprocessableEntity(new { erro = "Origem ou destino não encontrado." });
            rota = await maps.CalcularAsync(origem, destino, ct);
        }
        catch (HttpRequestException)
        {
            return StatusCode(502, new { erro = "Serviço de rotas indisponível." });
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(504, new { erro = "Tempo de resposta dos mapas excedido." });
        }

        if (rota is null || rota.DistanciaKm <= 0)
            return UnprocessableEntity(new { erro = "Não foi possível calcular a rota." });

        var v = new Viagem
        {
            CondutorId = id,
            Origem = input.Origem.Trim(),
            Destino = input.Destino.Trim(),
            PartidaEm = input.PartidaEm,
            LugaresOferecidos = input.Lugares,
            LugaresDisponiveis = input.Lugares,
            OrigemLatitude = rota.Origem.Latitude,
            OrigemLongitude = rota.Origem.Longitude,
            DestinoLatitude = rota.Destino.Latitude,
            DestinoLongitude = rota.Destino.Longitude,
            DistanciaKm = rota.DistanciaKm,
            DuracaoMinutos = rota.DuracaoMinutos
        };
        db.Viagens.Add(v);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Consultar), new { id = v.Id },
            new { v.Id, v.Origem, v.Destino, v.PartidaEm, v.LugaresDisponiveis, v.DistanciaKm, v.DuracaoMinutos });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Consultar(Guid id, CancellationToken ct)
    {
        var viagem = await db.Viagens.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        if (viagem is null) return NotFound();

        var participacoesAceites = await db.Participacoes.AsNoTracking()
            .Where(p => p.ViagemId == id && p.Estado == EstadoParticipacao.Aceite)
            .ToListAsync(ct);

        ResultadoEstimativaCusto? estimativa = null;
        string? motivoIndisponibilidade = null;

        if (custos is null)
        {
            // Compatibilidade com os testes legados que criam este controlador sem DI.
            motivoIndisponibilidade = "Serviço de estimativa não disponível.";
        }
        else
        {
            decimal? distanciaParaEstimativa = null;

            // Sem passageiros, a distância da rota original guardada é suficiente.
            if (participacoesAceites.Count == 0 && viagem.DistanciaKm > 0)
            {
                distanciaParaEstimativa = viagem.DistanciaKm;
            }
            else if (!maps.Configurado)
            {
                motivoIndisponibilidade = "Serviço de mapas não configurado.";
            }
            else
            {
                try
                {
                    // A rota completa é calculada internamente; os seus pontos não são expostos.
                    var resultado = await (consultaRotas ?? new ConsultaRotaViagemService(maps))
                        .CalcularAsync(viagem, participacoesAceites, ct);
                    distanciaParaEstimativa = resultado?.DistanciaTotalKm;
                }
                catch (HttpRequestException)
                {
                    motivoIndisponibilidade = "Serviço de mapas indisponível.";
                }
                catch (TaskCanceledException) when (!ct.IsCancellationRequested)
                {
                    motivoIndisponibilidade = "Tempo de resposta do serviço de mapas excedido.";
                }
            }

            if (distanciaParaEstimativa is > 0)
            {
                // Erros de configuração não são silenciosamente convertidos em valores indicativos.
                estimativa = custos.Calcular(distanciaParaEstimativa.Value, participacoesAceites.Count);
            }
            else
            {
                motivoIndisponibilidade ??= "Dados de distância insuficientes para calcular a estimativa.";
            }
        }

        return Ok(new
        {
            viagem.Id,
            viagem.Origem,
            viagem.Destino,
            viagem.PartidaEm,
            viagem.LugaresDisponiveis,
            viagem.Estado,
            viagem.DistanciaKm,
            viagem.DuracaoMinutos,
            viagem.CondutorId,
            EstimativaCusto = estimativa,
            EstimativaIndisponivelMotivo = motivoIndisponibilidade
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, EditarViagem input, CancellationToken ct)
    {
        if (Id is not Guid userId) return Unauthorized();
        var v = await db.Viagens.FindAsync([id], ct);
        if (v is null) return NotFound();
        if (v.CondutorId != userId) return Forbid();
        if (v.Estado != "Ativa" || v.PartidaEm <= DateTime.UtcNow)
            return Conflict(new { erro = "Viagem não editável." });
        if (input.PartidaEm.Kind != DateTimeKind.Utc || input.PartidaEm <= DateTime.UtcNow ||
            input.Lugares is < 1 or > 8)
            return BadRequest();

        var ocupados = v.LugaresOferecidos - v.LugaresDisponiveis;
        if (input.Lugares < ocupados)
            return Conflict(new { erro = "Lugares inferiores às participações aceites." });

        v.PartidaEm = input.PartidaEm;
        v.LugaresOferecidos = input.Lugares;
        v.LugaresDisponiveis = input.Lugares - ocupados;
        await db.SaveChangesAsync(ct);

        //Notifica os passageiros com participação aceite na viagem alterada.
        if (notificacaoService is not null) {
            var passageiros = await db.Participacoes
                .Where(p =>
                    p.ViagemId == id &&
                    p.Estado == EstadoParticipacao.Aceite)
                .Select(p => p.PassageiroId)
                .Distinct()
                .ToListAsync(ct);

            foreach (var passageiroId in passageiros)
            {
                await notificacaoService.CriarAsync(
                    passageiroId,
                    TipoNotificacao.AlteracaoViagem,
                    "Os dados da viagem em que participas foram alterados.",
                    ct);
            }
        }

        return NoContent();
    }

    [HttpPost("{id:guid}/indisponibilizar")]
    public async Task<IActionResult> Indisponibilizar(Guid id, CancellationToken ct)
    {
        if (Id is not Guid userId) return Unauthorized();
        var v = await db.Viagens.FindAsync([id], ct);
        if (v is null) return NotFound();
        if (v.CondutorId != userId) return Forbid();
        if (v.Estado != "Ativa") return Conflict();
        v.Estado = "Indisponibilizada";
        var pendentes = await db.Participacoes
            .Where(p => p.ViagemId == id && p.Estado == EstadoParticipacao.Pendente)
            .ToListAsync(ct);
        foreach (var p in pendentes) p.Estado = EstadoParticipacao.Cancelada;
        await db.SaveChangesAsync(ct);

        // Notifica os passageiros com participação aceite.
        if (notificacaoService is not null) {
            var passageiros = await db.Participacoes
                .Where(p =>
                    p.ViagemId == id &&
                    p.Estado == EstadoParticipacao.Aceite)
                .Select(p => p.PassageiroId)
                .Distinct()
                .ToListAsync(ct);

            foreach (var passageiroId in passageiros) {
                await notificacaoService.CriarAsync(
                    passageiroId,
                    TipoNotificacao.AlteracaoViagem,
                    "A viagem em que participas foi marcada como indisponível pelo condutor.",
                    ct);
            }
        }
        return NoContent();
    }

    [HttpPost("{id:guid}/concluir")]
    public async Task<IActionResult> Concluir(Guid id, CancellationToken ct)
    {
        if (Id is not Guid userId) return Unauthorized();
        var v = await db.Viagens.FindAsync([id], ct);
        if (v is null) return NotFound();
        if (v.CondutorId != userId) return Forbid();
        if (v.Estado != "Ativa" || v.PartidaEm > DateTime.UtcNow) return Conflict();
        v.Estado = "Concluída";
        var pendentes = await db.Participacoes
            .Where(p => p.ViagemId == id && p.Estado == EstadoParticipacao.Pendente)
            .ToListAsync(ct);
        foreach (var p in pendentes) p.Estado = EstadoParticipacao.Cancelada;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
    
}
