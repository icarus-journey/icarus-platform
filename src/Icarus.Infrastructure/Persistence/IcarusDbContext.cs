using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence;

/// <summary>
/// Contexto principal de acesso a dados da aplicação Icarus.
/// </summary>
public class IcarusDbContext : DbContext
{
    public IcarusDbContext(DbContextOptions<IcarusDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IcarusDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
