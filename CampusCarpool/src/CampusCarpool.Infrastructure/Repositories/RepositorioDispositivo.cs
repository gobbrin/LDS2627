using CampusCarpool.Application.Interfaces;
using CampusCarpool.Domain.Entities;
using CampusCarpool.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace CampusCarpool.Infrastructure.Repositories;

/// <summary>Implementação EF Core de <see cref="IRepositorioDispositivos"/>.</summary>
/// <remarks>O token é único na BD (índice único em <c>Dispositivos.Token</c>).</remarks>
public sealed class RepositorioDispositivo(CampusCarpoolDbContext db) : IRepositorioDispositivos
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Dispositivo>> ListarAtivosAsync(Guid id, CancellationToken ct) =>
        await db.Dispositivos.AsNoTracking().Where(d => d.UtilizadorId == id && d.Ativo).ToListAsync(ct);
    /// <inheritdoc />
    public async Task RegistarAsync(Guid id, string token, CancellationToken ct)
    {
        // Um token FCM identifica uma instalação; nunca pode pertencer a duas contas.
        var dispositivo = await db.Dispositivos.SingleOrDefaultAsync(d => d.Token == token, ct);
        if (dispositivo is null) db.Dispositivos.Add(new Dispositivo { UtilizadorId = id, Token = token, Ativo = true });
        else { dispositivo.UtilizadorId = id; dispositivo.Ativo = true; }
        await db.SaveChangesAsync(ct);
    }
    /// <inheritdoc />
    public async Task DesativarAsync(Guid id, string token, CancellationToken ct)
    {
        var d = await db.Dispositivos.SingleOrDefaultAsync(x => x.UtilizadorId == id && x.Token == token, ct);
        if (d is not null) { d.Ativo = false; await db.SaveChangesAsync(ct); }
    }
    /// <inheritdoc />
    public async Task DesativarTokenInvalidoAsync(string token, CancellationToken ct)
    {
        var d = await db.Dispositivos.SingleOrDefaultAsync(x => x.Token == token, ct);
        if (d is not null) { d.Ativo = false; await db.SaveChangesAsync(ct); }
    }
}
