using Icarus.Application.UseCases.ConsultarExtratoPontos;

namespace Icarus.Application.Interfaces;

public interface IExtratoPontosRepository
{
    Task<LoteExtratoPontos> ListarAsync(Guid titular, FiltroExtratoPontos filtro, DateTimeOffset referencia,
        string fuso, CursorExtratoPontos? cursor, CancellationToken ct);
    Task<MovimentoExtratoPontos?> ConsultarAsync(Guid titular, long id, string fuso, CancellationToken ct);
}
public interface ICursorExtratoPontos
{
    string Codificar(CursorExtratoPontos cursor);
    CursorExtratoPontos Decodificar(string cursor);
}
