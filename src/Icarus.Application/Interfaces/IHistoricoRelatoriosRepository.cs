using Icarus.Application.UseCases.ConsultarHistoricoRelatorios;
namespace Icarus.Application.Interfaces;

public interface IHistoricoRelatoriosRepository
{
    Task<LoteHistoricoRelatorios> ListarAsync(Guid titular, int limite, DateTimeOffset referencia, CursorHistoricoRelatorios? cursor, CancellationToken ct);
    Task<DetalheRelatorioSalvo?> ConsultarAsync(Guid titular, long id, CancellationToken ct);
}
public interface ICursorHistoricoRelatorios
{
    string Codificar(CursorHistoricoRelatorios cursor);
    CursorHistoricoRelatorios Decodificar(string cursor);
}
