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

/// <summary>
/// Ciclo de vida das viagens (F002): criar, consultar, editar, indisponibilizar e concluir. Rotas em <c>/api/v1/viagens</c>.
/// </summary>
/// <remarks>
/// Usa o <see cref="CampusCarpoolDbContext"/> diretamente; as transições de estado estão aqui e não no Domain
/// (DO-01). Os serviços opcionais (<c>= null</c>) existem para os testes antigos que criam o controller sem DI;
/// em execução normal vêm sempre preenchidos. Datas: a API só aceita <c>PartidaEm</c> em UTC
/// (<c>Kind = Utc</c>, ou seja, com <c>Z</c> no JSON) e recusa offsets como <c>+01:00</c> (DC-42).
/// Faltam, e ficam para m4: listar as viagens do condutor (DEF-BE-010), alterar origem e destino (DEF-BE-007),
/// nome e média do condutor nos detalhes (DEF-BE-011) e lista de passageiros (DEF-BE-012).
/// </remarks>
[ApiController, Authorize, Route("api/v1/viagens")]
public sealed class ViagensController(
    CampusCarpoolDbContext db,
    MapasService maps,
    EstimativaCustoService? custos = null,
    ConsultaRotaViagemService? consultaRotas = null,
    INotificacaoService? notificacaoService = null) : ControllerBase
{
    /// <summary>Corpo de <c>POST /viagens</c>. <c>PartidaEm</c> em UTC; <c>Lugares</c> de 1 a 8 (P-11).</summary>
    public sealed record CriarViagem(string Origem, string Destino, DateTime PartidaEm, int Lugares);
    /// <summary>Corpo de <c>PUT /viagens/{id}</c>: só a data/hora e os lugares podem mudar (DEF-BE-007).</summary>
    public sealed record EditarViagem(DateTime PartidaEm, int Lugares);

    // Id do utilizador autenticado (claim "sub"); null se o token não o tiver.
    private Guid? Id => Guid.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
        out var id) ? id : null;

    /// <summary>Cria uma viagem <c>Ativa</c> com o utilizador como condutor (RF-F002-01 a RF-F002-06).</summary>
    /// <remarks>
    /// Geocodifica a origem e o destino e calcula a rota no Serviço de Mapas (RF-F008-01, RF-F008-02); sem ele a
    /// criação é recusada (RF-F002-17, P-19). Guarda as coordenadas, a distância e a duração.
    /// </remarks>
    /// <returns>
    /// 201 viagem criada; 400 dados inválidos (vazios, mais de 250 caracteres, origem igual ao destino só pelo
    /// texto, data passada ou não UTC, lugares fora de 1 a 8); 401; 403 conta por confirmar; 422 local ou rota não
    /// encontrados; 502 serviço de mapas com erro; 503 serviço de mapas não configurado; 504 tempo esgotado.
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> Criar(CriarViagem input, CancellationToken ct)
    {
        if (Id is not Guid id) return Unauthorized();
        if (!await db.Utilizadores.AnyAsync(u => u.Id == id && u.EmailConfirmado, ct))
            return Forbid();
        // DO-11: "mesmo local" compara só o texto (RF-F002-04), embora as coordenadas sejam obtidas logo a seguir.
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

    /// <summary>Detalhes de uma viagem com a estimativa de custo por participante (RF-F003-06, RF-F008-08, RF-F008-10).</summary>
    /// <remarks>
    /// Qualquer utilizador autenticado pode consultar qualquer viagem, em qualquer estado. A estimativa usa a
    /// distância guardada quando não há passageiros; com passageiros aceites, recalcula a rota com os seus pontos
    /// (sem os expor, RF-F003-08). Se não for possível, devolve <c>EstimativaCusto = null</c> e o motivo em
    /// <c>EstimativaIndisponivelMotivo</c> (RF-F008-12), em vez de falhar o pedido.
    /// </remarks>
    /// <returns>200 detalhes; 401; 404 viagem inexistente.</returns>
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

    /// <summary>Altera a data/hora e os lugares de uma viagem ativa ainda por partir (RF-F002-07, RF-F002-08, RF-F002-16).</summary>
    /// <remarks>
    /// Os lugares não podem descer abaixo dos já ocupados; os disponíveis são recalculados. Notifica os passageiros
    /// aceites (Alteração da viagem, RF-F006-04). Concorrência: em SQL Server a coluna <c>Versao</c> faz falhar a
    /// segunda de duas gravações simultâneas; como aqui se chama <c>SaveChangesAsync</c> diretamente (sem a unidade de
    /// trabalho), a <c>DbUpdateConcurrencyException</c> chega ao cliente como 500, não como 409 (DC-41, fica para m4).
    /// O mesmo acontece em <see cref="Indisponibilizar"/> e <see cref="Concluir"/>.
    /// </remarks>
    /// <returns>204; 400 data passada, não UTC, ou lugares fora de 1 a 8; 401; 403 não é o condutor; 404; 409 viagem não editável ou lugares abaixo dos ocupados.</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, EditarViagem input, CancellationToken ct)
    {
        if (Id is not Guid userId) return Unauthorized();
        var v = await db.Viagens.FindAsync([id], ct);
        if (v is null) return NotFound();
        if (v.CondutorId != userId) return Forbid();
        if (v.Estado != nameof(EstadoViagem.Ativa) || v.PartidaEm <= DateTime.UtcNow)
            return Conflict(new { erro = "Viagem não editável." });
        if (input.PartidaEm.Kind != DateTimeKind.Utc || input.PartidaEm <= DateTime.UtcNow ||
            input.Lugares is < 1 or > 8)
            return BadRequest();

        // Ocupados = aceites (RF-F002-13), deduzidos dos valores guardados em vez de contados.
        var ocupados = v.LugaresOferecidos - v.LugaresDisponiveis;
        if (input.Lugares < ocupados)
            return Conflict(new { erro = "Lugares inferiores às participações aceites." });

        v.PartidaEm = input.PartidaEm;
        v.LugaresOferecidos = input.Lugares;
        v.LugaresDisponiveis = input.Lugares - ocupados;
        await db.SaveChangesAsync(ct);

        //Notifica os passageiros com participação aceite na viagem alterada.
        if (notificacaoService is not null)
        {
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

    /// <summary>Indisponibiliza uma viagem ativa (RF-F002-09, RF-F002-10, P-25).</summary>
    /// <remarks>
    /// Cancela os pedidos pendentes e as participações aceites, liberta todos os lugares e notifica os passageiros
    /// afetados, não o condutor (P-23, RF-F006-03). A viagem fica no histórico dos aceites graças a <c>AceiteEm</c>.
    /// Permitido mesmo depois da hora de partida, desde que ainda esteja <c>Ativa</c>.
    /// </remarks>
    /// <returns>204; 401; 403 não é o condutor; 404; 409 viagem já não está ativa.</returns>
    [HttpPost("{id:guid}/indisponibilizar")]
    public async Task<IActionResult> Indisponibilizar(Guid id, CancellationToken ct)
    {
        if (Id is not Guid userId) return Unauthorized();
        var v = await db.Viagens.FindAsync([id], ct);
        if (v is null) return NotFound();
        if (v.CondutorId != userId) return Forbid();
        if (v.Estado != nameof(EstadoViagem.Ativa)) return Conflict();
        v.Estado = nameof(EstadoViagem.Indisponibilizada);

        // P-25 / RF-F002-10 (DEF-BE-008): cancela os pedidos pendentes e as participações aceites.
        var afetadas = await db.Participacoes
            .Where(p => p.ViagemId == id &&
                (p.Estado == EstadoParticipacao.Pendente || p.Estado == EstadoParticipacao.Aceite))
            .ToListAsync(ct);
        foreach (var p in afetadas) p.Estado = EstadoParticipacao.Cancelada;
        // Sem participações aceites, todos os lugares ficam livres.
        v.LugaresDisponiveis = v.LugaresOferecidos;
        await db.SaveChangesAsync(ct);

        // P-23 / RF-F006-03 (DEF-BE-020): notifica os passageiros com pedido pendente ou
        // participação aceite antes da indisponibilização; o condutor originou a alteração.
        if (notificacaoService is not null)
        {
            var passageiros = afetadas
                .Select(p => p.PassageiroId)
                .Distinct();

            foreach (var passageiroId in passageiros)
            {
                await notificacaoService.CriarAsync(
                    passageiroId,
                    TipoNotificacao.AlteracaoViagem,
                    "A viagem foi marcada como indisponível pelo condutor e o teu pedido ou participação foi cancelado.",
                    ct);
            }
        }
        return NoContent();
    }

    /// <summary>Conclusão manual pelo condutor, depois da hora de partida.</summary>
    /// <remarks>
    /// Faz o mesmo que a conclusão automática (<c>ConclusaoViagensService</c>, RF-F002-12): passa a <c>Concluida</c> e
    /// cancela os pendentes (RF-F004-19); os aceites mantêm-se para a avaliação (P-16). Não envia notificações.
    /// Mantida até o grupo decidir PA-01/PA-03.
    /// </remarks>
    /// <returns>204; 401; 403 não é o condutor; 404; 409 viagem não ativa ou ainda por partir.</returns>
    [HttpPost("{id:guid}/concluir")]
    public async Task<IActionResult> Concluir(Guid id, CancellationToken ct)
    {
        if (Id is not Guid userId) return Unauthorized();
        var v = await db.Viagens.FindAsync([id], ct);
        if (v is null) return NotFound();
        if (v.CondutorId != userId) return Forbid();
        if (v.Estado != nameof(EstadoViagem.Ativa) || v.PartidaEm > DateTime.UtcNow) return Conflict();
        v.Estado = nameof(EstadoViagem.Concluida);
        var pendentes = await db.Participacoes
            .Where(p => p.ViagemId == id && p.Estado == EstadoParticipacao.Pendente)
            .ToListAsync(ct);
        foreach (var p in pendentes) p.Estado = EstadoParticipacao.Cancelada;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

}
