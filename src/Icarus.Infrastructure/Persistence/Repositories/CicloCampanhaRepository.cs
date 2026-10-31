using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Infrastructure.Persistence.Repositories;

public sealed class CicloCampanhaRepository(IcarusDbContext contexto) : ICicloCampanhaRepository
{
    public Task<List<string?>> ListarFusosPlanejadosAsync(Guid? usuarioId, CancellationToken ct)
        => contexto.Campanhas.Where(c => c.Estado == EstadoCampanha.Planejada && c.Epico.Estado != EstadoEpico.Excluido &&
                (usuarioId == null || c.Epico.UsuarioId == usuarioId))
            .Select(c => c.Epico.Usuario.FusoHorario).Distinct().ToListAsync(ct);

    public Task<int> IniciarPlanejadasAsync(Guid? usuarioId, string? fusoHorario, DateOnly hoje, DateTimeOffset agora, CancellationToken ct)
        => contexto.Campanhas.Where(c => c.Estado == EstadoCampanha.Planejada && c.Epico.Estado != EstadoEpico.Excluido && c.PrazoInicio <= hoje &&
                c.Epico.Usuario.FusoHorario == fusoHorario && (usuarioId == null || c.Epico.UsuarioId == usuarioId))
            .ExecuteUpdateAsync(atualizacao => atualizacao
                .SetProperty(c => c.Estado, EstadoCampanha.EmAndamento)
                .SetProperty(c => c.AtualizadoEm, agora), ct);

    public Task<List<Missao>> ListarRecorrentesParaEncerrarAsync(long campanhaId, Guid usuarioId, DateOnly hoje, CancellationToken ct)
        => contexto.Missoes.Where(m => m.CampanhaId == campanhaId && m.UsuarioId == usuarioId && m.Recorrencia != null && m.Status != StatusMissao.Excluida)
            .Include(m => m.Recorrencia)
            .Include(m => m.Ocorrencias.Where(o => o.DataPrevista >= hoje &&
                (o.Status == StatusOcorrencia.Pendente || o.Status == StatusOcorrencia.Atrasada)))
            .ToListAsync(ct);
}
