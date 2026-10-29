using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public sealed class DiarioRepository(IcarusDbContext contexto) : IDiarioRepository
{
    public async Task AdicionarAsync(Diario entrada, CancellationToken cancellationToken) =>
        await contexto.Diarios.AddAsync(entrada, cancellationToken);

    public Task<List<Diario>> ListarAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        contexto.Diarios.AsNoTracking()
            .Where(entrada => entrada.UsuarioId == usuarioId && entrada.ExcluidaEm == null)
            .OrderByDescending(entrada => entrada.DataRegistro)
            .ThenByDescending(entrada => entrada.CriadoEm)
            .ThenByDescending(entrada => entrada.Id)
            .ToListAsync(cancellationToken);

    public Task<Diario?> BuscarPorIdAsync(long entradaId, Guid usuarioId, CancellationToken cancellationToken) =>
        contexto.Diarios.SingleOrDefaultAsync(entrada => entrada.Id == entradaId && entrada.UsuarioId == usuarioId,
            cancellationToken);
}
