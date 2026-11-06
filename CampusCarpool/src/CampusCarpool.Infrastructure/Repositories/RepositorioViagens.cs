using CampusCarpool.Application.Abstractions.Persistence;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Domain.Enums;
using CampusCarpool.Domain.Rules;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusCarpool.Infrastructure.Repositories;

/// <summary>Implementação EF Core de <see cref="IRepositorioViagens"/>.</summary>
public sealed class RepositorioViagens(CampusCarpoolDbContext context) : IRepositorioViagens
{
    /// <inheritdoc />
    /// <remarks>
    /// Obtém uma viagem pelo seu identificador.
    /// A entidade fica rastreada para permitir alterações posteriores; a <c>Versao</c> lida aqui é a que o
    /// SQL Server compara ao gravar.
    /// </remarks>
    public async Task<Viagem?> ObterPorIdAsync(Guid viagemId, CancellationToken ct)
    {
        return await context.Viagens.FirstOrDefaultAsync(v => v.Id == viagemId, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ViagemEmCurso>> ListarAtivasJaPartidasAsync(DateTime agora, CancellationToken ct)
    {
        return await context.Viagens
            .AsNoTracking()
            .Where(v => v.Estado == nameof(EstadoViagem.Ativa) && v.PartidaEm <= agora)
            .Select(v => new ViagemEmCurso(v.Id, v.PartidaEm, v.DuracaoMinutos))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// DC-42 / API-06 (fica para m4): o dia e a hora são comparados em UTC, não na hora de Portugal; perto da
    /// meia-noite ou com hora de verão, uma viagem pode cair no dia ou hora errados.
    /// </remarks>
    public async Task<PaginaViagens> PesquisarAsync(FiltroPesquisaViagens filtro, DateTime agora, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var q = context.Viagens.AsNoTracking()
            .Where(v => v.Estado == nameof(EstadoViagem.Ativa) && v.LugaresDisponiveis > 0 && v.PartidaEm > agora);

        if (!string.IsNullOrWhiteSpace(filtro.Origem))
        {
            var termo = TextoPesquisa.Normalizar(filtro.Origem);
            q = q.Where(v => v.OrigemPesquisa.Contains(termo));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Destino))
        {
            var termo = TextoPesquisa.Normalizar(filtro.Destino);
            q = q.Where(v => v.DestinoPesquisa.Contains(termo));
        }

        if (filtro.Data is DateOnly data)
        {
            var inicio = data.ToDateTime(TimeOnly.MinValue);
            var fim = inicio.AddDays(1);
            q = q.Where(v => v.PartidaEm >= inicio && v.PartidaEm < fim);
        }

        if (filtro.Hora is TimeOnly hora)
        {
            var minutos = hora.Hour * 60 + hora.Minute;
            q = q.Where(v => v.PartidaEm.Hour * 60 + v.PartidaEm.Minute >= minutos);
        }

        var total = await q.CountAsync(ct);

        var viagens = await q
            .OrderBy(v => v.PartidaEm)
            .ThenBy(v => v.Id)
            .Skip((filtro.Pagina - 1) * filtro.Tamanho)
            .Take(filtro.Tamanho)
            .Select(v => new ViagemPesquisada(
                v.Id, v.Origem, v.Destino, v.PartidaEm, v.LugaresDisponiveis,
                v.DistanciaKm, v.DuracaoMinutos, v.CondutorId, v.RotaPontos))
            .ToListAsync(ct);

        return new PaginaViagens(total, viagens);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ViagemDoCondutor>> ListarAtivasDoCondutorAsync(Guid condutorId, CancellationToken ct)
    {
        return await context.Viagens
            .AsNoTracking()
            .Where(v => v.CondutorId == condutorId && v.Estado == nameof(EstadoViagem.Ativa))
            .OrderBy(v => v.PartidaEm)
            .Select(v => new ViagemDoCondutor(v.Id, v.Origem, v.Destino, v.PartidaEm, v.LugaresOferecidos, v.LugaresDisponiveis))
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public void Adicionar(Viagem viagem) => context.Viagens.Add(viagem);
}
