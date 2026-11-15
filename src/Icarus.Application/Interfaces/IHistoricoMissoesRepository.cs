using Icarus.Application.UseCases.ConsultarHistoricoMissoes;

namespace Icarus.Application.Interfaces;

public interface IHistoricoMissoesRepository
{
    Task<LoteHistoricoMissoes> ListarAsync(Guid titular, FiltroHistoricoMissoes filtro, DateTimeOffset referencia,
        string fuso, CursorHistoricoMissoes? cursor, CancellationToken ct);
    Task<RegistroHistoricoMissao?> ConsultarAsync(Guid titular, long ocorrenciaId, DateOnly hoje, CancellationToken ct);
    Task<IReadOnlyList<CampanhaHistoricoMissao>> ListarCampanhasAsync(Guid titular, CancellationToken ct);
}
public interface ICursorHistoricoMissoes
{
    string Codificar(CursorHistoricoMissoes cursor);
    CursorHistoricoMissoes Decodificar(string cursor);
}
