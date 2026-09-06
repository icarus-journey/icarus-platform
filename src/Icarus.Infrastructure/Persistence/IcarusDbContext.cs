using Icarus.Domain.Common;
using Icarus.Domain.Entities;
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

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rotina> Rotinas => Set<Rotina>();
    public DbSet<Epico> Epicos => Set<Epico>();
    public DbSet<Campanha> Campanhas => Set<Campanha>();
    public DbSet<Missao> Missoes => Set<Missao>();
    public DbSet<MissaoIA> MissoesIA => Set<MissaoIA>();
    public DbSet<Recorrencia> Recorrencias => Set<Recorrencia>();
    public DbSet<Diario> Diarios => Set<Diario>();
    public DbSet<Relatorio> Relatorios => Set<Relatorio>();
    public DbSet<Item> Itens => Set<Item>();
    public DbSet<InventarioItem> InventarioItens => Set<InventarioItem>();
    public DbSet<MovimentacaoPontos> MovimentacoesPontos => Set<MovimentacaoPontos>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IcarusDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        AtualizarTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AtualizarTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void AtualizarTimestamps()
    {
        var agora = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<EntidadeBase>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CriadoEm = agora;
                entry.Entity.AtualizadoEm = agora;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.AtualizadoEm = agora;
            }
        }

        foreach (var entry in ChangeTracker.Entries<MovimentacaoPontos>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CriadoEm = agora;
            }
        }
    }
}
