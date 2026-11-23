using CampusCarpool.Application.Comum;
using CampusCarpool.Application.Viagens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusCarpool.Api.Controllers;

/// <summary>
/// Ciclo de vida das viagens (F002): criar, consultar, editar, indisponibilizar e concluir. Rotas em <c>/api/v1/viagens</c>.
/// </summary>
/// <remarks>
/// As regras estão nos serviços: <see cref="IViagemService"/> (criar, consultar, listar, editar),
/// <see cref="ISimulacaoDeAlteracaoService"/> (simular a alteração) e <see cref="ICicloDeVidaDaViagemService"/>
/// (indisponibilizar e concluir).
/// </remarks>
[ApiController, Authorize, Route("api/v1/viagens")]
public sealed class ViagensController(
    IViagemService viagens,
    ISimulacaoDeAlteracaoService simulacoes,
    ICicloDeVidaDaViagemService cicloDeVida) : ControllerBase
{
    /// <summary>Corpo de <c>POST /viagens</c>. <c>PartidaEm</c> em UTC; <c>Lugares</c> de 1 a 8 (P-11).</summary>
    public sealed record CriarViagem(string Origem, string Destino, DateTime PartidaEm, int Lugares);
    /// <summary>Corpo de <c>PUT /viagens/{id}</c>. <c>Origem</c> e <c>Destino</c> a <c>null</c> mantêm o valor atual.</summary>
    public sealed record EditarViagem(DateTime PartidaEm, int Lugares, string? Origem = null, string? Destino = null);
    /// <summary>Corpo de <c>POST /viagens/{id}/simular-alteracao</c>. Como no PUT, um local a <c>null</c> mantém o atual.</summary>
    public sealed record SimularAlteracaoViagem(string? Origem = null, string? Destino = null);

    /// <summary>Cria uma viagem <c>Ativa</c> com o utilizador como condutor (RF-F002-01 a RF-F002-06).</summary>
    /// <returns>
    /// 201 viagem criada; 400 dados inválidos (vazios, mais de 200 caracteres, origem e destino no mesmo local,
    /// data passada ou não UTC, lugares fora de 1 a 8); 401; 422 local ou rota não
    /// encontrados; 502 serviço de mapas com erro; 503 serviço de mapas não configurado; 504 tempo esgotado.
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> Criar(CriarViagem input, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid id) return Unauthorized();

        return this.Responder(
            await viagens.CriarAsync(id, input.Origem, input.Destino, input.PartidaEm, input.Lugares, ct),
            viagem => CreatedAtAction(nameof(Consultar), new { id = viagem.Id }, viagem));
    }

    /// <summary>Detalhes de uma viagem com a estimativa de custo por participante (RF-F003-06, RF-F008-08, RF-F008-10).</summary>
    /// <returns>200 detalhes; 401; 404 viagem inexistente ou que o utilizador não pode consultar.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Consultar(Guid id, CancellationToken ct) =>
        this.Responder(await viagens.ConsultarAsync(id, User.IdUtilizador(), ct), detalhes => Ok(detalhes));

    /// <summary>Viagens ativas do condutor autenticado, por ordem de partida, para as gerir (RF-F002-15, DEF-BE-010).</summary>
    /// <remarks>As viagens noutros estados estão no histórico (RF-F009-02).</remarks>
    /// <returns>200 <see cref="PaginaViagensDoCondutor"/> (<c>{ total, pagina, tamanho, viagens }</c>); 400 paginação inválida; 401.</returns>
    [HttpGet("minhas")]
    public async Task<IActionResult> ListarMinhas(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = Paginacao.TamanhoPorOmissao,
        CancellationToken ct = default)
    {
        if (User.IdUtilizador() is not Guid userId) return Unauthorized();

        // D-4: a lista é paginada; a paginação valida-se aqui (BE-41).
        if (Paginacao.Validar(pagina, tamanho) is { } paginacaoInvalida)
            return this.RespostaDeErro(paginacaoInvalida);

        return Ok(await viagens.ListarDoCondutorAsync(userId, pagina, tamanho, ct));
    }

    /// <summary>Passageiros aceites de uma viagem, com os respetivos pontos (RF-F003-07, P-13, DEF-BE-012).</summary>
    /// <remarks>Um ponto a <c>null</c> segue a origem ou o destino da viagem (P-20).</remarks>
    /// <returns>200 lista; 401; 403 não é o condutor nem passageiro aceite; 404.</returns>
    [HttpGet("{id:guid}/passageiros")]
    public async Task<IActionResult> ListarPassageiros(Guid id, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid userId) return Unauthorized();

        return this.Responder(await viagens.ListarPassageirosAsync(id, userId, ct), passageiros => Ok(passageiros));
    }

    /// <summary>Pedidos pendentes de uma viagem, do mais antigo para o mais recente (RF-F004-08, DEF-BE-016).</summary>
    /// <returns>200 lista; 401; 403 não é o condutor; 404.</returns>
    [HttpGet("{id:guid}/pedidos")]
    public async Task<IActionResult> ListarPedidosPendentes(Guid id, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid userId) return Unauthorized();

        return this.Responder(await viagens.ListarPedidosPendentesAsync(id, userId, ct), pedidos => Ok(pedidos));
    }

    /// <summary>Altera a origem, o destino, a data/hora e os lugares de uma viagem ativa ainda por partir (RF-F002-07, RF-F002-08, RF-F002-16).</summary>
    /// <remarks>Ver <see cref="IViagemService.EditarAsync"/>.</remarks>
    /// <returns>
    /// 204; 400 dados inválidos (locais vazios, com mais de 200 caracteres ou no mesmo local, data passada ou não UTC,
    /// lugares fora de 1 a 8); 401; 403 não é o condutor; 404; 409 viagem não editável, lugares abaixo dos ocupados ou
    /// alteração simultânea; 422 local ou rota não encontrados; 502/503/504 problema no serviço de mapas.
    /// </returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, EditarViagem input, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid userId) return Unauthorized();

        return this.Responder(
            await viagens.EditarAsync(id, userId, input.PartidaEm, input.Lugares, input.Origem, input.Destino, ct),
            () => NoContent());
    }

    /// <summary>
    /// Simula a alteração da origem ou do destino, sem gravar: rota com as paragens dos passageiros aceites,
    /// distância, duração e custo, para o ecrã de edição mostrar antes de guardar (RF-F002-07, RF-F008-05, RF-F008-10).
    /// </summary>
    /// <remarks>Ver <see cref="ISimulacaoDeAlteracaoService.SimularAlteracaoAsync"/>.</remarks>
    /// <returns>
    /// 200 rota simulada (mesmos campos de <c>GET /rotas/viagens/{id}</c>); 400 locais vazios, com mais de 200
    /// caracteres ou no mesmo local; 401; 403 não é o condutor; 404; 409 viagem não editável; 422 local ou rota não
    /// encontrados; 502/503/504 problema no serviço de mapas.
    /// </returns>
    [HttpPost("{id:guid}/simular-alteracao")]
    public async Task<IActionResult> SimularAlteracao(Guid id, SimularAlteracaoViagem input, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid userId) return Unauthorized();

        return this.Responder(
            await simulacoes.SimularAlteracaoAsync(id, userId, input.Origem, input.Destino, ct),
            rota => Ok(rota));
    }

    /// <summary>Indisponibiliza uma viagem ativa (RF-F002-09, RF-F002-10, P-25).</summary>
    /// <remarks>Ver <see cref="ICicloDeVidaDaViagemService.IndisponibilizarAsync"/>.</remarks>
    /// <returns>204; 401; 403 não é o condutor; 404; 409 viagem já não está ativa.</returns>
    [HttpPost("{id:guid}/indisponibilizar")]
    public async Task<IActionResult> Indisponibilizar(Guid id, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid userId) return Unauthorized();

        return this.Responder(await cicloDeVida.IndisponibilizarAsync(id, userId, ct), () => NoContent());
    }

    /// <summary>Conclusão manual pelo condutor, depois da hora de partida.</summary>
    /// <remarks>Ver <see cref="ICicloDeVidaDaViagemService.ConcluirAsync"/>.</remarks>
    /// <returns>204; 401; 403 não é o condutor; 404; 409 viagem não ativa ou ainda por partir.</returns>
    [HttpPost("{id:guid}/concluir")]
    public async Task<IActionResult> Concluir(Guid id, CancellationToken ct)
    {
        if (User.IdUtilizador() is not Guid userId) return Unauthorized();

        return this.Responder(await cicloDeVida.ConcluirAsync(id, userId, ct), () => NoContent());
    }
}
