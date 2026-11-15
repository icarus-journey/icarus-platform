using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public class OcorrenciaRepository(IcarusDbContext context) : IOcorrenciaRepository
{
    public async Task AdicionarAsync(Ocorrencia ocorrencia, CancellationToken cancellationToken)
        => await context.Ocorrencias.AddAsync(ocorrencia, cancellationToken);

    public Task<Ocorrencia?> BuscarPorMissaoEDataAsync(long missaoId, DateOnly dataPrevista, CancellationToken cancellationToken)
        => context.Ocorrencias.SingleOrDefaultAsync(o => o.MissaoId == missaoId && o.DataPrevista == dataPrevista, cancellationToken);

    public async Task<Ocorrencia?> BuscarParaConcluirAsync(long missaoId, DateOnly hoje, CancellationToken cancellationToken)
    {
        var deHoje = await context.Ocorrencias.Include(o => o.MovimentacoesPontos)
            .SingleOrDefaultAsync(o => o.MissaoId == missaoId && o.DataPrevista == hoje && o.Status != StatusOcorrencia.Excluida, cancellationToken);
        if (deHoje is not null)
        {
            return deHoje;
        }

        var pendente = await context.Ocorrencias
            .Where(o => o.MissaoId == missaoId && o.DataPrevista <= hoje && (o.Status == StatusOcorrencia.Pendente || o.Status == StatusOcorrencia.Atrasada))
            .OrderBy(o => o.DataPrevista)
            .FirstOrDefaultAsync(cancellationToken);
        if (pendente is not null)
        {
            return pendente;
        }

        // Nenhuma pendente nem "de hoje": se a missão (tipicamente não recorrente, com
        // data_prevista diferente de hoje) já tem uma concluída, devolve a mais recente —
        // garante idempotência mesmo quando a data prevista não coincide com o dia da chamada.
        return await context.Ocorrencias.Include(o => o.MovimentacoesPontos)
            .Where(o => o.MissaoId == missaoId && o.DataPrevista <= hoje && (o.Status == StatusOcorrencia.Concluida || o.Status == StatusOcorrencia.ConcluidaComAtraso))
            .OrderByDescending(o => o.DataPrevista)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<DateOnly>> ListarDatasExistentesAsync(long missaoId, DateOnly de, DateOnly ate, CancellationToken cancellationToken)
        => await context.Ocorrencias
            .Where(o => o.MissaoId == missaoId && o.DataPrevista >= de && o.DataPrevista <= ate)
            .Select(o => o.DataPrevista)
            .ToListAsync(cancellationToken);

    public async Task<List<Ocorrencia>> ListarPendentesNaoVencidasAsync(long missaoId, DateOnly hoje, CancellationToken cancellationToken)
        => await context.Ocorrencias
            .Where(o => o.MissaoId == missaoId && (o.Status == StatusOcorrencia.Pendente || o.Status == StatusOcorrencia.Atrasada) && o.DataPrevista >= hoje)
            .ToListAsync(cancellationToken);
}
