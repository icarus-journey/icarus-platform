namespace Icarus.Application.Interfaces;

public interface IIniciadorCampanhas
{
    Task<int> ExecuteAsync(Guid? usuarioId, CancellationToken ct);
}
