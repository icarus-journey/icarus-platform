using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public sealed class RelatorioSemanalRepository(IcarusDbContext contexto) : IRelatorioSemanalRepository
{
    public async Task ExecutarAsync(Guid usuarioId, Func<Usuario, CancellationToken, Task> operacao, CancellationToken ct)
    {
        await using var transacao = await contexto.Database.BeginTransactionAsync(ct);
        var usuario = await contexto.Usuarios.FromSqlInterpolated($"SELECT * FROM usuario WHERE id={usuarioId} FOR UPDATE").SingleOrDefaultAsync(ct)
            ?? throw new PerfilNaoEncontradoException();
        await operacao(usuario, ct);
        await transacao.CommitAsync(ct);
    }
    public Task<TrabalhoRelatorioSemanal?> BuscarTrabalhoAsync(Guid usuarioId, DateOnly inicio, DateOnly fim, CancellationToken ct)
        => contexto.Set<TrabalhoRelatorioSemanal>().SingleOrDefaultAsync(t => t.UsuarioId == usuarioId && t.PeriodoInicio == inicio && t.PeriodoFim == fim, ct);
    public Task<TrabalhoRelatorioSemanal?> BuscarPorIdAsync(Guid id, CancellationToken ct)
        => contexto.Set<TrabalhoRelatorioSemanal>().SingleOrDefaultAsync(t => t.Id == id, ct);
    public Task<Guid?> BuscarTitularAsync(Guid id, CancellationToken ct)
        => contexto.Set<TrabalhoRelatorioSemanal>().Where(t => t.Id == id).Select(t => (Guid?)t.UsuarioId).SingleOrDefaultAsync(ct);
    public Task<Relatorio?> BuscarRelatorioAsync(Guid usuarioId, DateOnly inicio, DateOnly fim, CancellationToken ct)
        => contexto.Relatorios.AsNoTracking().SingleOrDefaultAsync(r => r.UsuarioId == usuarioId && r.Tipo == TipoRelatorio.Semanal && r.PeriodoInicio == inicio && r.PeriodoFim == fim, ct);
    public Task<bool> ResultadoRecebidoAsync(Guid idMensagem, CancellationToken ct) => contexto.Set<InboxRelatorio>().AnyAsync(i => i.Id == idMensagem, ct);
    public async Task AdicionarTrabalhoAsync(TrabalhoRelatorioSemanal trabalho, CancellationToken ct) => await contexto.AddAsync(trabalho, ct);
    public async Task AdicionarOutboxAsync(OutboxRelatorio mensagem, CancellationToken ct) => await contexto.AddAsync(mensagem, ct);
    public async Task AdicionarInboxAsync(InboxRelatorio mensagem, CancellationToken ct) => await contexto.AddAsync(mensagem, ct);
    public async Task AdicionarRelatorioAsync(Relatorio relatorio, CancellationToken ct) => await contexto.AddAsync(relatorio, ct);
}
