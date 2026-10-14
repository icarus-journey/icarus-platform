using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public class UsuarioRepository(IcarusDbContext context) : IUsuarioRepository
{
    public Task<bool> ExisteComEmailAsync(string email, CancellationToken cancellationToken)
        => context.Usuarios.AnyAsync(u => u.Email == email, cancellationToken);

    public Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken cancellationToken)
        => context.Usuarios.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<Usuario?> BuscarPorIdAsync(Guid id, CancellationToken cancellationToken)
        => context.Usuarios.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task AdicionarUsuarioAsync(Usuario usuario, CancellationToken cancellationToken)
        => await context.Usuarios.AddAsync(usuario, cancellationToken);
}
